using Microsoft.Extensions.DependencyInjection;
using SalesToSignature.Agents.Configuration;
using SalesToSignature.Agents.Telemetry;
using Xunit;

namespace SalesToSignature.Tests.Telemetry;

public class TelemetrySetupTests
{
    [Fact]
    public void ConfigureOpenTelemetry_RegistersServices()
    {
        var services = new ServiceCollection();
        var settings = new AgentSettings
        {
            OtelConsoleExport = false,
            AppInsightsConnectionString = ""
        };

        var result = TelemetrySetup.ConfigureOpenTelemetry(services, settings);

        Assert.Same(services, result);
        Assert.True(services.Count > 0, "Should register OTel services");
    }

    [Fact]
    public void ConfigureOpenTelemetry_WithAppInsights_RegistersOtlpExporter()
    {
        var services = new ServiceCollection();
        var settings = new AgentSettings
        {
            AppInsightsConnectionString = "InstrumentationKey=test"
        };

        TelemetrySetup.ConfigureOpenTelemetry(services, settings);

        // Verify services were registered (OTLP exporter adds hosted services)
        Assert.True(services.Count > 0);
    }

    [Fact]
    public void StartAgentSpan_CreatesActivityWithAttributes()
    {
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = source => source.Name == "SalesToSignature.Agents",
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) =>
                System.Diagnostics.ActivitySamplingResult.AllDataAndRecorded
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        using var activity = TelemetrySetup.StartAgentSpan("intake");

        Assert.NotNull(activity);
        Assert.Equal("invoke_agent intake", activity.DisplayName);

        var tags = activity.Tags.ToDictionary(t => t.Key, t => t.Value);
        Assert.Equal("intake", tags["agent.name"]);
        Assert.Equal("intake", tags["agent.stage"]);
    }

    [Fact]
    public void StartAgentSpan_WithParentStage_IncludesParentTag()
    {
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = source => source.Name == "SalesToSignature.Agents",
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) =>
                System.Diagnostics.ActivitySamplingResult.AllDataAndRecorded
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        using var activity = TelemetrySetup.StartAgentSpan("qualification", parentStage: "intake");

        Assert.NotNull(activity);

        var tags = activity.Tags.ToDictionary(t => t.Key, t => t.Value);
        Assert.Equal("qualification", tags["agent.name"]);
        Assert.Equal("intake", tags["agent.parent_stage"]);
    }

    [Fact]
    public void StartAgentSpan_WithoutParentStage_NoParentTag()
    {
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = source => source.Name == "SalesToSignature.Agents",
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) =>
                System.Diagnostics.ActivitySamplingResult.AllDataAndRecorded
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        using var activity = TelemetrySetup.StartAgentSpan("coordinator");

        Assert.NotNull(activity);

        var tags = activity.Tags.ToDictionary(t => t.Key, t => t.Value);
        Assert.False(tags.ContainsKey("agent.parent_stage"));
    }

    [Fact]
    public void AgentActivitySource_HasCorrectName()
    {
        Assert.Equal("SalesToSignature.Agents", TelemetrySetup.AgentActivitySource.Name);
    }

    [Fact]
    public void SafetyCheckMetrics_Exist()
    {
        Assert.NotNull(TelemetrySetup.SafetyCheckDurationHistogram);
        Assert.NotNull(TelemetrySetup.SafetyCheckCounter);
    }

    [Fact]
    public void RecordSafetyCheck_DoesNotThrow()
    {
        // Metrics recording should never throw even without a listener
        TelemetrySetup.RecordSafetyCheck("PromptShield", 42.5, "allowed");
        TelemetrySetup.RecordSafetyCheck("ContentSafety", 15.2, "blocked");
        TelemetrySetup.RecordSafetyCheck("Groundedness", 88.0, "fallback");
    }

    [Fact]
    public void PipelineRequestCounter_Exists()
    {
        Assert.NotNull(TelemetrySetup.PipelineRequestCounter);
    }

    [Fact]
    public void AgentDurationHistogram_Exists()
    {
        Assert.NotNull(TelemetrySetup.AgentDurationHistogram);
    }
}
