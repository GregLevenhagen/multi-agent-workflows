using System.Text.Json.Serialization;

namespace SalesToSignature.Agents.Models;

/// <summary>
/// Human-in-the-loop approval decision from the Approval agent.
/// Captures the reviewer's sign-off or rejection via the <c>ApproveContract</c> tool,
/// which pauses the pipeline for human review.
/// </summary>
public record ApprovalDecision
{
    [JsonPropertyName("approved")]
    public bool Approved { get; init; }

    [JsonPropertyName("reviewerName")]
    public string ReviewerName { get; init; } = string.Empty;

    [JsonPropertyName("feedback")]
    public string Feedback { get; init; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; }

    [JsonPropertyName("contractSummary")]
    public string ContractSummary { get; init; } = string.Empty;
}
