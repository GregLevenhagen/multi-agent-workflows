using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SalesToSignature.Agents.Models;

/// <summary>
/// Structured data extracted from an RFP by the Intake agent.
/// Represents the parsed opportunity that flows to the Qualification agent for scoring.
/// Implements <see cref="IValidatableObject"/> for cross-property validation at pipeline boundaries.
/// </summary>
public record OpportunityRecord : IValidatableObject
{
    [JsonPropertyName("clientName")]
    public string ClientName { get; init; } = string.Empty;

    [JsonPropertyName("engagementType")]
    public EngagementType EngagementType { get; init; }

    [JsonPropertyName("budgetMin")]
    public decimal BudgetMin { get; init; }

    [JsonPropertyName("budgetMax")]
    public decimal BudgetMax { get; init; }

    [JsonPropertyName("timelineStart")]
    public DateTime TimelineStart { get; init; }

    [JsonPropertyName("timelineEnd")]
    public DateTime TimelineEnd { get; init; }

    [JsonPropertyName("techStack")]
    public List<string> TechStack { get; init; } = [];

    [JsonPropertyName("keyRequirements")]
    public List<string> KeyRequirements { get; init; } = [];

    [JsonPropertyName("rawDocumentText")]
    public string RawDocumentText { get; init; } = string.Empty;

    /// <summary>Confidence score (0.0–1.0) of the engagement type classification.</summary>
    [JsonPropertyName("classificationConfidence")]
    [Range(0.0, 1.0)]
    public double ClassificationConfidence { get; init; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (BudgetMin > BudgetMax)
        {
            yield return new ValidationResult(
                "BudgetMin must be less than or equal to BudgetMax.",
                [nameof(BudgetMin), nameof(BudgetMax)]);
        }

        if (TimelineEnd != default && TimelineStart != default && TimelineStart > TimelineEnd)
        {
            yield return new ValidationResult(
                "TimelineStart must be before or equal to TimelineEnd.",
                [nameof(TimelineStart), nameof(TimelineEnd)]);
        }

        if (ClassificationConfidence < 0.0 || ClassificationConfidence > 1.0)
        {
            yield return new ValidationResult(
                "ClassificationConfidence must be between 0.0 and 1.0.",
                [nameof(ClassificationConfidence)]);
        }
    }
}
