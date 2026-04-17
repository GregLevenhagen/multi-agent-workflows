namespace SalesToSignature.Agents.Configuration;

/// <summary>
/// Strongly-typed settings for the Sales-to-Signature agent pipeline.
/// Binds to environment variables for Azure AI Foundry, Content Safety, and telemetry.
/// </summary>
public class AgentSettings
{
    public string ProjectEndpoint { get; set; } = string.Empty;
    public string ProjectKey { get; set; } = string.Empty;
    public string ModelDeploymentName { get; set; } = "gpt-4o";
    public string ContentSafetyEndpoint { get; set; } = string.Empty;
    public string ContentSafetyKey { get; set; } = string.Empty;
    public string AppInsightsConnectionString { get; set; } = string.Empty;
    public bool OtelConsoleExport { get; set; }
    public string Environment { get; set; } = "development";
    public int ContentSafetySeverityThreshold { get; set; } = 4;
}
