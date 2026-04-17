using System.Text.Json.Serialization;

namespace SalesToSignature.Agents.Models;

/// <summary>
/// Classification of how the consulting engagement is structured and billed.
/// Determines which SOW template, rate card, and engagement-specific clauses to use.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EngagementType
{
    /// <summary>Dedicated team members placed at the client site, billed by role/hour.</summary>
    StaffAugmentation,

    /// <summary>Defined scope with a fixed total price and milestone-based payments.</summary>
    FixedBid,

    /// <summary>Hourly/daily billing with flexible scope adjustments.</summary>
    TimeAndMaterials,

    /// <summary>Strategic consulting and assessment engagements.</summary>
    Advisory
}
