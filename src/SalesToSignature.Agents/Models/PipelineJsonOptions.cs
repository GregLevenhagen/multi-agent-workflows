using System.Text.Json;

namespace SalesToSignature.Agents.Models;

/// <summary>
/// Shared <see cref="JsonSerializerOptions"/> for the pipeline.
/// Centralizes serialization defaults so all agents, tools, and tests
/// use the same configuration. Cached as static readonly to satisfy CA1869.
/// </summary>
public static class PipelineJsonOptions
{
    /// <summary>
    /// Default options for deserializing pipeline models.
    /// Case-insensitive property matching handles camelCase JSON from agents.
    /// </summary>
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Options for producing human-readable JSON output (e.g., tool responses, logging).
    /// </summary>
    public static readonly JsonSerializerOptions Indented = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
}
