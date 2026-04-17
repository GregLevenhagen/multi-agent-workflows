using System.Text.Json.Serialization;

namespace SalesToSignature.Agents.Models;

/// <summary>Outcome of the Review agent's cross-check of proposal and contract against the RFP.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReviewStatus
{
    /// <summary>No issues found — ready for approval.</summary>
    Clean,

    /// <summary>Issues detected — routes back to Contract or Proposal agent for remediation.</summary>
    IssuesFound
}

/// <summary>Severity classification for issues found during review.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IssueSeverity
{
    /// <summary>Minor cosmetic or stylistic concern.</summary>
    Low,

    /// <summary>Notable issue that should be addressed but is not blocking.</summary>
    Medium,

    /// <summary>Significant problem requiring remediation before approval.</summary>
    High,

    /// <summary>Deal-breaking issue that must be resolved immediately.</summary>
    Critical
}

/// <summary>A single issue flagged during the Review agent's cross-check.</summary>
public record ReviewIssue
{
    [JsonPropertyName("severity")]
    public IssueSeverity Severity { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; init; } = string.Empty;
}

/// <summary>Tracks whether a specific RFP requirement is addressed in the proposal/contract.</summary>
public record RequirementCheck
{
    [JsonPropertyName("requirement")]
    public string Requirement { get; init; } = string.Empty;

    [JsonPropertyName("covered")]
    public bool Covered { get; init; }

    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}

/// <summary>
/// Review agent output: cross-checks the contract and proposal against the original RFP.
/// When <see cref="OverallStatus"/> is <see cref="ReviewStatus.IssuesFound"/>,
/// <see cref="TargetAgent"/> indicates which agent should remediate (contract or proposal).
/// </summary>
public record ReviewReport
{
    [JsonPropertyName("overallStatus")]
    public ReviewStatus OverallStatus { get; init; }

    [JsonPropertyName("issues")]
    public List<ReviewIssue> Issues { get; init; } = [];

    [JsonPropertyName("pricingConsistent")]
    public bool PricingConsistent { get; init; }

    [JsonPropertyName("requirementsCovered")]
    public List<RequirementCheck> RequirementsCovered { get; init; } = [];

    [JsonPropertyName("targetAgent")]
    public string? TargetAgent { get; init; }
}
