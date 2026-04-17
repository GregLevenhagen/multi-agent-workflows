using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SalesToSignature.Agents.Configuration;

namespace SalesToSignature.Agents.Telemetry;

public static class TelemetrySetup
{
    public static readonly ActivitySource AgentActivitySource = new("SalesToSignature.Agents");
    private static readonly Meter AgentMeter = new("SalesToSignature.Agents", "1.0.0");

    /// <summary>Counts pipeline requests by outcome (e.g., completed, rejected, error).</summary>
    public static readonly Counter<long> PipelineRequestCounter =
        AgentMeter.CreateCounter<long>("pipeline.requests", "requests", "Total pipeline requests processed");

    /// <summary>Records agent stage duration in milliseconds.</summary>
    public static readonly Histogram<double> AgentDurationHistogram =
        AgentMeter.CreateHistogram<double>("agent.duration", "ms", "Agent stage execution duration");

    /// <summary>Records safety middleware check latency in milliseconds.</summary>
    public static readonly Histogram<double> SafetyCheckDurationHistogram =
        AgentMeter.CreateHistogram<double>("safety.check.duration", "ms", "Safety middleware check duration");

    /// <summary>Counts safety middleware outcomes (blocked, allowed, fallback).</summary>
    public static readonly Counter<long> SafetyCheckCounter =
        AgentMeter.CreateCounter<long>("safety.checks", "checks", "Safety middleware check outcomes");

    public static IServiceCollection ConfigureOpenTelemetry(
        IServiceCollection services,
        AgentSettings settings)
    {
        var serviceVersion = typeof(TelemetrySetup).Assembly.GetName().Version?.ToString() ?? "0.0.0";

        // Resolve OTLP endpoint: env var override → default AI Toolkit local endpoint
        var otlpEndpoint = System.Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
            ?? "http://localhost:4318";
        var enableOtlp = !string.IsNullOrEmpty(settings.AppInsightsConnectionString)
            || !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: "SalesToSignature.Agents",
                    serviceVersion: serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = settings.Environment,
                    ["service.namespace"] = "sales-to-signature"
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource("SalesToSignature.Agents")
                    .AddSource("Microsoft.Agents.AI.*")
                    .AddSource("Microsoft.Extensions.AI")
                    .SetSampler(new AlwaysOnSampler());

                // OTLP exporter for VS Code AI Toolkit — HTTP/protobuf to port 4318
                if (enableOtlp)
                {
                    tracing.AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri("http://localhost:4318/v1/traces");
                        o.Protocol = OtlpExportProtocol.HttpProtobuf;
                    });
                }

                // Console exporter for local debugging
                if (settings.OtelConsoleExport)
                {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter("SalesToSignature.Agents");

                if (enableOtlp)
                {
                    metrics.AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri("http://localhost:4318/v1/metrics");
                        o.Protocol = OtlpExportProtocol.HttpProtobuf;
                    });
                }

                if (settings.OtelConsoleExport)
                {
                    metrics.AddConsoleExporter();
                }
            });

        return services;
    }

    /// <summary>
    /// Starts a new activity (span) for an agent stage with standard and GenAI semantic convention attributes.
    /// GenAI attributes are required by the VS Code AI Toolkit tracing viewer.
    /// </summary>
    public static Activity? StartAgentSpan(string agentName, string? parentStage = null)
    {
        var activity = AgentActivitySource.StartActivity(
            $"invoke_agent {agentName}", ActivityKind.Internal);
        if (activity != null)
        {
            // GenAI semantic conventions (required for AI Toolkit tracing viewer)
            activity.SetTag("gen_ai.operation.name", "invoke_agent");
            activity.SetTag("gen_ai.system", "openai");
            activity.SetTag("gen_ai.provider.name", "azure.ai.inference");
            activity.SetTag("gen_ai.agent.name", agentName);

            // Custom attributes
            activity.SetTag("agent.name", agentName);
            activity.SetTag("agent.stage", agentName);
            if (parentStage != null)
                activity.SetTag("agent.parent_stage", parentStage);

        }
        return activity;
    }

    /// <summary>
    /// Records the duration and outcome of a safety middleware check (Prompt Shield, Groundedness, Content Safety).
    /// </summary>
    public static void RecordSafetyCheck(string checkType, double durationMs, string outcome)
    {
        SafetyCheckDurationHistogram.Record(durationMs,
            new KeyValuePair<string, object?>("safety.check.type", checkType));
        SafetyCheckCounter.Add(1,
            new KeyValuePair<string, object?>("safety.check.type", checkType),
            new KeyValuePair<string, object?>("safety.check.outcome", outcome));
    }
}
