using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SalesToSignature.Agents.Models;

/// <summary>Go/NoGo recommendation from the Qualification agent.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Recommendation
{
    /// <summary>Proceed — opportunity meets qualification criteria.</summary>
    Go,

    /// <summary>Decline — deal-breakers or insufficient fit detected.</summary>
    NoGo
}

/// <summary>
/// Qualification scoring output from the Qualification agent.
/// Scores fit and risk on a 1–10 scale and makes a Go/NoGo recommendation
/// grounded solely in the <see cref="OpportunityRecord"/> data.
/// Implements <see cref="IValidatableObject"/> for cross-property validation at pipeline boundaries.
/// </summary>
public record QualificationResult : IValidatableObject
{
    /// <summary>How well the opportunity matches firm capabilities (1 = poor, 10 = ideal).</summary>
    [JsonPropertyName("fitScore")]
    [Range(1, 10)]
    public int FitScore { get; init; }

    /// <summary>Overall risk level of the engagement (1 = low risk, 10 = very high risk).</summary>
    [JsonPropertyName("riskScore")]
    [Range(1, 10)]
    public int RiskScore { get; init; }

    [JsonPropertyName("revenuePotential")]
    public decimal RevenuePotential { get; init; }

    [JsonPropertyName("requiredSkills")]
    public List<string> RequiredSkills { get; init; } = [];

    [JsonPropertyName("risks")]
    public List<string> Risks { get; init; } = [];

    [JsonPropertyName("dealBreakers")]
    public List<string> DealBreakers { get; init; } = [];

    [JsonPropertyName("recommendation")]
    public Recommendation Recommendation { get; init; }

    [JsonPropertyName("reasoning")]
    public string Reasoning { get; init; } = string.Empty;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FitScore < 1 || FitScore > 10)
        {
            yield return new ValidationResult(
                "FitScore must be between 1 and 10.",
                [nameof(FitScore)]);
        }

        if (RiskScore < 1 || RiskScore > 10)
        {
            yield return new ValidationResult(
                "RiskScore must be between 1 and 10.",
                [nameof(RiskScore)]);
        }

        if (Recommendation == Recommendation.NoGo && string.IsNullOrWhiteSpace(Reasoning))
        {
            yield return new ValidationResult(
                "Reasoning is required when Recommendation is NoGo.",
                [nameof(Reasoning)]);
        }
    }
}
