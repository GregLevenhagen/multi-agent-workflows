using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;
using SalesToSignature.Agents.Models;

namespace SalesToSignature.Agents.Tools;

public class PricingCalculator
{

    private readonly string _dataDirectory;

    public PricingCalculator(string? dataDirectory = null)
    {
        _dataDirectory = dataDirectory
            ?? Environment.GetEnvironmentVariable("DATA_DIRECTORY")
            ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data");
    }

    [Description("Calculates pricing for an engagement based on roles, engagement type, and duration. Returns a JSON pricing breakdown with per-role costs and total.")]
    public AIFunction CreateTool()
    {
        return AIFunctionFactory.Create(CalculatePricing);
    }

    [Description("Calculates engagement pricing based on roles, engagement type, and duration in months.")]
    public string CalculatePricing(
        [Description("The engagement type: StaffAugmentation, FixedBid, TimeAndMaterials, or Advisory")] string engagementType,
        [Description("Comma-separated list of roles needed (e.g., 'Architect,SeniorDev,SeniorDev,Developer')")] string roles,
        [Description("Duration of the engagement in months (must be > 0)")] int durationMonths)
    {
        if (durationMonths <= 0)
            return "Error: durationMonths must be greater than 0";

        if (string.IsNullOrWhiteSpace(roles))
            return "Error: roles must not be empty";

        if (string.IsNullOrWhiteSpace(engagementType))
            return "Error: engagementType must not be empty";

        var rateCardPath = Path.Combine(_dataDirectory, "pricing", "rate-cards.json");

        if (!File.Exists(rateCardPath))
            return $"Error: Rate card file not found at {rateCardPath}";

        var rateCardJson = File.ReadAllText(rateCardPath);
        using var doc = JsonDocument.Parse(rateCardJson);

        var rolesRoot = doc.RootElement.GetProperty("roles");
        var metadata = doc.RootElement.GetProperty("metadata");
        var hoursPerMonth = metadata.GetProperty("hoursPerMonth").GetInt32();

        var roleList = roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var pricingLines = new List<object>();
        var total = 0m;

        foreach (var roleName in roleList)
        {
            var roleEntry = FindRole(rolesRoot, roleName);
            if (roleEntry == null)
            {
                pricingLines.Add(new { role = roleName, error = $"Unknown role '{roleName}'" });
                continue;
            }

            var rates = roleEntry.Value.GetProperty("rates");
            if (!rates.TryGetProperty(engagementType, out var engagementRates))
            {
                pricingLines.Add(new { role = roleName, error = $"No rates for engagement type '{engagementType}'" });
                continue;
            }

            var hourlyRate = engagementRates.GetProperty("hourly").GetDecimal();
            var totalHours = hoursPerMonth * durationMonths;
            var subtotal = hourlyRate * totalHours;
            total += subtotal;

            pricingLines.Add(new
            {
                role = roleName,
                title = roleEntry.Value.GetProperty("title").GetString(),
                hourlyRate,
                hoursPerMonth,
                totalHours,
                durationMonths,
                subtotal
            });
        }

        // Apply volume discount if available
        var discount = FindVolumeDiscount(doc.RootElement, durationMonths);
        var discountAmount = 0m;
        string? discountLabel = null;
        var discountPercent = 0;

        if (discount != null)
        {
            discountPercent = discount.Value.GetProperty("discountPercent").GetInt32();
            if (discountPercent > 0)
            {
                discountLabel = discount.Value.GetProperty("label").GetString();
                discountAmount = total * discountPercent / 100m;
            }
        }

        var result = new
        {
            engagementType,
            durationMonths,
            pricingLines,
            subtotalBeforeDiscount = total,
            volumeDiscount = new
            {
                label = discountLabel ?? "Standard",
                percent = discountPercent,
                amount = discountAmount
            },
            totalPrice = total - discountAmount,
            currency = metadata.GetProperty("currency").GetString()
        };

        return JsonSerializer.Serialize(result, PipelineJsonOptions.Indented);
    }

    private static JsonElement? FindVolumeDiscount(JsonElement root, int durationMonths)
    {
        if (!root.TryGetProperty("volumeDiscounts", out var discounts))
            return null;

        foreach (var tier in discounts.EnumerateArray())
        {
            var min = tier.GetProperty("minMonths").GetInt32();
            var max = tier.GetProperty("maxMonths").GetInt32();
            if (durationMonths >= min && durationMonths <= max)
                return tier;
        }

        return null;
    }

    private static JsonElement? FindRole(JsonElement rolesArray, string roleName)
    {
        foreach (var role in rolesArray.EnumerateArray())
        {
            if (role.GetProperty("role").GetString()?.Equals(roleName, StringComparison.OrdinalIgnoreCase) == true)
                return role;
        }
        return null;
    }
}
