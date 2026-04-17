using System.Text.Json.Serialization;

namespace SalesToSignature.Agents.Models;

/// <summary>
/// Legal contract assembled by the Contract agent from MSA/NDA templates,
/// standard clauses, and engagement-specific clauses via <c>LegalTemplateLookup</c>
/// and <c>ClauseLibrary</c>.
/// </summary>
public record ContractDocument
{
    [JsonPropertyName("contractText")]
    public string ContractText { get; init; } = string.Empty;

    [JsonPropertyName("standardClauses")]
    public List<string> StandardClauses { get; init; } = [];

    [JsonPropertyName("customClauses")]
    public List<string> CustomClauses { get; init; } = [];

    [JsonPropertyName("effectiveDate")]
    public DateTime EffectiveDate { get; init; }

    [JsonPropertyName("terminationTerms")]
    public string TerminationTerms { get; init; } = string.Empty;

    [JsonPropertyName("liabilityCap")]
    public decimal LiabilityCap { get; init; }

    [JsonPropertyName("paymentTerms")]
    public string PaymentTerms { get; init; } = string.Empty;
}
