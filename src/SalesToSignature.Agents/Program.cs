using System.Diagnostics;
using System.Text.Json;
using Azure;
using Azure.AI.ContentSafety;
using Azure.AI.Inference;
using Azure.Identity;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SalesToSignature.Agents.Configuration;
using SalesToSignature.Agents.Models;
using SalesToSignature.Agents.Orchestration;
using SalesToSignature.Agents.Safety;
using SalesToSignature.Agents.Telemetry;

var builder = WebApplication.CreateBuilder(args);

// Bind configuration from environment variables
var settings = new AgentSettings
{
    ProjectEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT") ?? "",
    ProjectKey = Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_KEY") ?? "",
    ModelDeploymentName = Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME") ?? "gpt-4o",
    ContentSafetyEndpoint = Environment.GetEnvironmentVariable("AZURE_CONTENT_SAFETY_ENDPOINT") ?? "",
    ContentSafetyKey = Environment.GetEnvironmentVariable("AZURE_CONTENT_SAFETY_KEY") ?? "",
    AppInsightsConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING") ?? "",
    OtelConsoleExport = Environment.GetEnvironmentVariable("OTEL_CONSOLE_EXPORT")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true,
    Environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "development"
};

builder.Services.AddSingleton(settings);

// Health checks (used by Docker HEALTHCHECK and orchestrators like Kubernetes)
builder.Services.AddHealthChecks();

// Configure OpenTelemetry
TelemetrySetup.ConfigureOpenTelemetry(builder.Services, settings);

// Register IChatClient via Azure AI Inference SDK with API key auth
if (!string.IsNullOrEmpty(settings.ProjectEndpoint) && !string.IsNullOrEmpty(settings.ProjectKey))
{
    var chatCompletionsClient = new ChatCompletionsClient(
        new Uri(settings.ProjectEndpoint), new AzureKeyCredential(settings.ProjectKey));
    builder.Services.AddSingleton<IChatClient>(
        chatCompletionsClient.AsIChatClient(settings.ModelDeploymentName)
            .AsBuilder()
            .UseOpenTelemetry(sourceName: "Microsoft.Extensions.AI")
            .Build());
}

// Register ContentSafetyClient and safety middleware
if (!string.IsNullOrEmpty(settings.ContentSafetyEndpoint) && !string.IsNullOrEmpty(settings.ContentSafetyKey))
{
    var contentSafetyClient = new ContentSafetyClient(
        new Uri(settings.ContentSafetyEndpoint),
        new AzureKeyCredential(settings.ContentSafetyKey));

    builder.Services.AddSingleton(contentSafetyClient);
    builder.Services.AddSingleton(sp => new ContentSafetyFilter(
        sp.GetRequiredService<ContentSafetyClient>(), settings));
    builder.Services.AddSingleton(sp => new PromptShieldMiddleware(
        settings.ContentSafetyEndpoint, settings.ContentSafetyKey));
    builder.Services.AddSingleton(sp => new GroundednessValidator(
        settings.ContentSafetyEndpoint, settings.ContentSafetyKey));
}

var app = builder.Build();

// Request logging
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RequestLog");
    logger.LogInformation("{Method} {Path}", context.Request.Method, context.Request.Path);
    var sw = System.Diagnostics.Stopwatch.StartNew();
    await next();
    sw.Stop();
    logger.LogInformation("{Method} {Path} → {Status} ({Elapsed}ms)",
        context.Request.Method, context.Request.Path, context.Response.StatusCode, sw.ElapsedMilliseconds);
});

// Health probe for Docker HEALTHCHECK and orchestrators (returns 200 with no body)
app.MapHealthChecks("/healthz");

// Service info endpoint
app.MapGet("/", () => Results.Ok(new
{
    service = "Sales-to-Signature Multi-Agent Pipeline",
    status = "healthy",
    version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0",
    environment = settings.Environment,
    hasChatClient = !string.IsNullOrEmpty(settings.ProjectEndpoint) && !string.IsNullOrEmpty(settings.ProjectKey),
    hasContentSafety = !string.IsNullOrEmpty(settings.ContentSafetyEndpoint)
}));

// Step 1.2: Raw HTTP POST test — can this process reach 4318 at all?
app.MapGet("/trace-raw", async () =>
{
    using var client = new HttpClient();
    // Minimal valid OTLP protobuf (empty ExportTraceServiceRequest)
    var emptyBody = Array.Empty<byte>();
    var content = new ByteArrayContent(emptyBody);
    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-protobuf");
    try
    {
        var resp = await client.PostAsync("http://localhost:4318/v1/traces", content);
        var body = await resp.Content.ReadAsStringAsync();
        Console.WriteLine($"[trace-raw] POST to 4318: {resp.StatusCode} - {body}");
        return Results.Ok(new { status = (int)resp.StatusCode, body });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[trace-raw] FAILED: {ex.Message}");
        return Results.Ok(new { error = ex.Message });
    }
});

// Diagnostic endpoint — bypasses DI-hosted OpenTelemetry entirely.
// Creates its own TracerProvider using the classic API (same pattern as the standalone console app that works).
app.MapGet("/trace-diag", () =>
{
    var diagSource = new System.Diagnostics.ActivitySource("diag-test");
    using var provider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("diag-test"))
        .AddSource("diag-test")
        .SetSampler(new AlwaysOnSampler())
        .AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri("http://localhost:4318/v1/traces");
            o.Protocol = OtlpExportProtocol.HttpProtobuf;
        })
        .Build();

    using (var activity = diagSource.StartActivity("chat gpt-4o", System.Diagnostics.ActivityKind.Client))
    {
        activity?.SetTag("gen_ai.operation.name", "chat");
        activity?.SetTag("gen_ai.system", "openai");
        activity?.SetTag("gen_ai.request.model", "gpt-4o");
        activity?.SetTag("gen_ai.usage.input_tokens", 10);
        activity?.SetTag("gen_ai.usage.output_tokens", 20);
    }

    var flushed = provider.ForceFlush();
    Console.WriteLine($"[trace-diag] ForceFlush returned: {flushed}");
    return Results.Ok(new { source = "classic-api-bypass", flushed });
});

// Trace test endpoint — makes a direct LLM call to generate gen_ai.* spans for the VS Code AI Toolkit viewer
app.MapGet("/trace-test", async (IServiceProvider sp) =>
{
    var chatClient = sp.GetService<IChatClient>();
    if (chatClient == null)
        return Results.Problem("IChatClient not configured", statusCode: 503);

    var response = await chatClient.GetResponseAsync("Say 'tracing works' in one sentence.");

    // Flush traces to the AI Toolkit collector immediately
    sp.GetService<TracerProvider>()?.ForceFlush();

    return Results.Ok(new { message = response.Text });
});

// Pipeline endpoint — POST /responses with {"input": "<RFP text>"}
// Streams NDJSON events as the workflow progresses through agents.
app.MapPost("/responses", async (HttpContext context, IServiceProvider sp) =>
{
    // Detach from ASP.NET's request Activity so our span becomes a root span.
    // Without this, our span is a child of an unexported ASP.NET span, making it
    // invisible in the AI Toolkit visualizer (which only shows root spans).
    var previousActivity = Activity.Current;
    Activity.Current = null;
    using var activity = TelemetrySetup.AgentActivitySource.StartActivity("chat gpt-4o",
        System.Diagnostics.ActivityKind.Server);
    activity?.SetTag("gen_ai.operation.name", "chat");
    activity?.SetTag("gen_ai.system", "openai");
    activity?.SetTag("gen_ai.provider.name", "azure.ai.inference");
    activity?.SetTag("gen_ai.request.model", settings.ModelDeploymentName);
    activity?.SetTag("http.method", "POST");
    activity?.SetTag("http.url", context.Request.Path);

    var sw = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        var chatClient = sp.GetService<IChatClient>();
        if (chatClient == null)
        {
            activity?.SetTag("pipeline.error", "IChatClient not configured");
            return Results.Problem(
                "IChatClient not configured. Set AZURE_AI_PROJECT_ENDPOINT or deploy as a Foundry hosted agent.",
                statusCode: 503);
        }

        // Parse input
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();

        JsonElement inputElement;
        try
        {
            using var input = JsonDocument.Parse(body);
            inputElement = input.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            activity?.SetTag("pipeline.error", "Invalid JSON");
            activity?.SetTag("pipeline.error_details", ex.Message);
            return Results.BadRequest(new { error = "Invalid JSON body. Expected: {\"input\": \"<RFP text>\"}" });
        }

        if (!inputElement.TryGetProperty("input", out var inputProp))
            return Results.BadRequest(new { error = "Missing 'input' property. Expected: {\"input\": \"<RFP text>\"}" });

        var rfpText = inputProp.GetString() ?? "";
        if (string.IsNullOrWhiteSpace(rfpText))
            return Results.BadRequest(new { error = "Input text is required" });

        activity?.SetTag("pipeline.input_length", rfpText.Length);

        // Check for prompt injection if Content Safety is configured
        var shieldMiddleware = sp.GetService<PromptShieldMiddleware>();
        var shieldedText = rfpText;

        if (shieldMiddleware != null)
        {
            using var shieldActivity = TelemetrySetup.AgentActivitySource.StartActivity("execute_tool prompt_shield");
            shieldActivity?.SetTag("gen_ai.operation.name", "execute_tool");
            shieldActivity?.SetTag("gen_ai.system", "azure");
            var shieldSw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Apply Prompt Shield spotlighting to untrusted input
                shieldedText = shieldMiddleware.ApplySpotlighting(rfpText);
                var shieldResult = await shieldMiddleware.AnalyzeDocumentAsync(rfpText);

                shieldSw.Stop();
                TelemetrySetup.RecordSafetyCheck("prompt_shield", shieldSw.Elapsed.TotalMilliseconds,
                    shieldResult.IsAttackDetected ? "blocked" : "allowed");

                if (shieldResult.IsAttackDetected)
                {
                    shieldActivity?.SetTag("safety.attack_detected", true);
                    shieldActivity?.SetTag("safety.attack_type", shieldResult.AttackType);
                    shieldActivity?.SetTag("safety.confidence", shieldResult.Confidence);
                    TelemetrySetup.PipelineRequestCounter.Add(1,
                        new KeyValuePair<string, object?>("pipeline.outcome", "rejected"));

                    return Results.Json(new
                    {
                        error = "Prompt injection detected",
                        attackType = shieldResult.AttackType,
                        confidence = shieldResult.Confidence
                    }, statusCode: 422);
                }

                shieldActivity?.SetTag("safety.attack_detected", false);
            }
            catch (Exception ex)
            {
                shieldSw.Stop();
                shieldActivity?.SetTag("safety.error", ex.GetType().Name);
                shieldActivity?.SetTag("safety.error_details", ex.Message);
                activity?.SetTag("pipeline.warning", "Prompt Shield check failed");
            }
        }

        // Build and invoke the pipeline workflow with streaming
        var loggerFactory = sp.GetService<ILoggerFactory>();

        using var buildActivity = TelemetrySetup.AgentActivitySource.StartActivity("invoke_agent pipeline");
        buildActivity?.SetTag("gen_ai.operation.name", "invoke_agent");
        buildActivity?.SetTag("gen_ai.system", "azure");
        buildActivity?.SetTag("gen_ai.agent.name", "pipeline");
        var workflow = PipelineBuilder.BuildPipeline(chatClient, loggerFactory: loggerFactory);
        buildActivity?.SetTag("pipeline.agents_count", 7); // coordinator, intake, qualification, proposal, contract, review, approval

        // Stream NDJSON events to the client as the workflow progresses
        context.Response.ContentType = "application/x-ndjson";
        context.Response.StatusCode = 200;

        var cancellationToken = context.RequestAborted;
        var eventCount = 0;

        await using var run = await InProcessExecution.Default
            .RunStreamingAsync(workflow, shieldedText, cancellationToken: cancellationToken);

        // Send TurnToken to trigger agent processing — agents cache messages until they receive this signal
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        using var streamActivity = TelemetrySetup.AgentActivitySource.StartActivity("invoke_agent stream");
        streamActivity?.SetTag("gen_ai.operation.name", "invoke_agent");
        streamActivity?.SetTag("gen_ai.system", "azure");
        streamActivity?.SetTag("gen_ai.agent.name", "stream");

        await foreach (var evt in run.WatchStreamAsync(cancellationToken))
        {
            eventCount++;

            var eventData = evt switch
            {
                AgentResponseEvent response => new
                {
                    type = "agent_response",
                    agent = response.ExecutorId,
                    messages = response.Response.Messages
                        .Select(m => new { role = m.Role.Value, content = m.Text })
                        .ToArray()
                } as object,
                AgentResponseUpdateEvent update => new
                {
                    type = "agent_update",
                    agent = update.ExecutorId,
                    content = update.Update.Text
                } as object,
                ExecutorEvent executorEvt => new
                {
                    type = "event",
                    name = evt.GetType().Name,
                    executor = executorEvt.ExecutorId
                } as object,
                _ => new
                {
                    type = "event",
                    name = evt.GetType().Name
                } as object
            };

            var line = JsonSerializer.Serialize(eventData, PipelineJsonOptions.Default);
            await context.Response.WriteAsync(line + "\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }

        sw.Stop();

        activity?.SetTag("pipeline.outcome", "completed");
        activity?.SetTag("pipeline.event_count", eventCount);
        activity?.SetTag("pipeline.duration_ms", sw.ElapsedMilliseconds);
        streamActivity?.SetTag("pipeline.events_streamed", eventCount);

        TelemetrySetup.PipelineRequestCounter.Add(1,
            new KeyValuePair<string, object?>("pipeline.outcome", "completed"));
        TelemetrySetup.AgentDurationHistogram.Record(sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("pipeline.stage", "full_pipeline"));

        // Flush traces to the AI Toolkit collector immediately
        sp.GetService<TracerProvider>()?.ForceFlush();

        return Results.Empty;
    }
    catch (OperationCanceledException)
    {
        activity?.SetTag("pipeline.outcome", "cancelled");
        TelemetrySetup.PipelineRequestCounter.Add(1,
            new KeyValuePair<string, object?>("pipeline.outcome", "cancelled"));
        return Results.Empty;
    }
    catch (Exception ex)
    {
        sw.Stop();
        activity?.SetTag("pipeline.error_type", ex.GetType().Name);
        activity?.SetTag("pipeline.error_details", ex.Message);
        activity?.SetTag("pipeline.outcome", "error");
        activity?.SetTag("pipeline.duration_ms", sw.ElapsedMilliseconds);

        TelemetrySetup.PipelineRequestCounter.Add(1,
            new KeyValuePair<string, object?>("pipeline.outcome", "error"));

        return Results.Problem(
            "An error occurred processing the pipeline",
            statusCode: 500);
    }
});

app.Run();

// Make the implicit Program class accessible for WebApplicationFactory<Program> in tests
public partial class Program { }
