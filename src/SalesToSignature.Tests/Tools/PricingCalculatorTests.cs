using System.Text.Json;
using SalesToSignature.Agents.Tools;
using Xunit;

namespace SalesToSignature.Tests.Tools;

public class PricingCalculatorTests
{
    private readonly PricingCalculator _calculator;

    public PricingCalculatorTests()
    {
        var dataDir = Path.Combine(FindRepoRoot(), "data");
        _calculator = new PricingCalculator(dataDir);
    }

    [Fact]
    public void CalculatePricing_SingleArchitect_StaffAugmentation_12Months()
    {
        var result = _calculator.CalculatePricing("StaffAugmentation", "Architect", 12);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;

        Assert.Equal("StaffAugmentation", root.GetProperty("engagementType").GetString());
        Assert.Equal(12, root.GetProperty("durationMonths").GetInt32());

        var lines = root.GetProperty("pricingLines");
        Assert.Equal(1, lines.GetArrayLength());

        var line = lines[0];
        Assert.Equal(275m, line.GetProperty("hourlyRate").GetDecimal());
        Assert.Equal(1920, line.GetProperty("totalHours").GetInt32()); // 160 * 12
        Assert.Equal(528000m, line.GetProperty("subtotal").GetDecimal()); // 275 * 1920

        // 12 months = Annual tier = 8% discount
        Assert.Equal(528000m, root.GetProperty("subtotalBeforeDiscount").GetDecimal());
        var discount = root.GetProperty("volumeDiscount");
        Assert.Equal("Annual", discount.GetProperty("label").GetString());
        Assert.Equal(8, discount.GetProperty("percent").GetInt32());
        Assert.Equal(42240m, discount.GetProperty("amount").GetDecimal()); // 528000 * 0.08
        Assert.Equal(485760m, root.GetProperty("totalPrice").GetDecimal()); // 528000 - 42240
        Assert.Equal("USD", root.GetProperty("currency").GetString());
    }

    [Fact]
    public void CalculatePricing_MultipleRoles_FixedBid()
    {
        var result = _calculator.CalculatePricing("FixedBid", "Architect,SeniorDev,Developer", 6);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;

        var lines = root.GetProperty("pricingLines");
        Assert.Equal(3, lines.GetArrayLength());

        // Architect FixedBid: 300/hr * 160 * 6 = 288,000
        Assert.Equal(288000m, lines[0].GetProperty("subtotal").GetDecimal());
        // SeniorDev FixedBid: 250/hr * 160 * 6 = 240,000
        Assert.Equal(240000m, lines[1].GetProperty("subtotal").GetDecimal());
        // Developer FixedBid: 200/hr * 160 * 6 = 192,000
        Assert.Equal(192000m, lines[2].GetProperty("subtotal").GetDecimal());

        // 6 months = Standard tier, no discount
        Assert.Equal(720000m, root.GetProperty("subtotalBeforeDiscount").GetDecimal());
        Assert.Equal(0, root.GetProperty("volumeDiscount").GetProperty("percent").GetInt32());
        Assert.Equal(720000m, root.GetProperty("totalPrice").GetDecimal());
    }

    [Theory]
    [InlineData("StaffAugmentation", "Architect", 275)]
    [InlineData("FixedBid", "Architect", 300)]
    [InlineData("TimeAndMaterials", "Architect", 285)]
    [InlineData("Advisory", "Architect", 300)]
    [InlineData("StaffAugmentation", "DevOpsEngineer", 235)]
    [InlineData("FixedBid", "DevOpsEngineer", 260)]
    public void CalculatePricing_AllEngagementTypes_CorrectRates(string engagementType, string role, int expectedHourly)
    {
        var result = _calculator.CalculatePricing(engagementType, role, 1);

        using var doc = JsonDocument.Parse(result);
        var line = doc.RootElement.GetProperty("pricingLines")[0];
        Assert.Equal(expectedHourly, line.GetProperty("hourlyRate").GetDecimal());
    }

    [Fact]
    public void CalculatePricing_UnknownRole_ReturnsError()
    {
        var result = _calculator.CalculatePricing("StaffAugmentation", "UnknownRole", 1);

        using var doc = JsonDocument.Parse(result);
        var lines = doc.RootElement.GetProperty("pricingLines");
        Assert.Equal(1, lines.GetArrayLength());
        Assert.Contains("Unknown role", lines[0].GetProperty("error").GetString());
        Assert.Equal(0m, doc.RootElement.GetProperty("totalPrice").GetDecimal());
    }

    [Fact]
    public void CalculatePricing_ZeroDuration_ReturnsError()
    {
        var result = _calculator.CalculatePricing("StaffAugmentation", "Architect", 0);

        Assert.Contains("durationMonths must be greater than 0", result);
    }

    [Fact]
    public void CalculatePricing_NegativeDuration_ReturnsError()
    {
        var result = _calculator.CalculatePricing("StaffAugmentation", "Architect", -3);

        Assert.Contains("durationMonths must be greater than 0", result);
    }

    [Fact]
    public void CalculatePricing_EmptyRoles_ReturnsError()
    {
        var result = _calculator.CalculatePricing("StaffAugmentation", "", 6);

        Assert.Contains("roles must not be empty", result);
    }

    [Theory]
    [InlineData(3, 0, "Standard")]   // 1-6 months: 0%
    [InlineData(6, 0, "Standard")]
    [InlineData(7, 5, "Extended")]   // 7-9 months: 5%
    [InlineData(9, 5, "Extended")]
    [InlineData(10, 8, "Annual")]    // 10-12 months: 8%
    [InlineData(12, 8, "Annual")]
    [InlineData(18, 12, "Strategic")] // 13-24 months: 12%
    public void CalculatePricing_VolumeDiscount_CorrectTier(int months, int expectedPercent, string expectedLabel)
    {
        var result = _calculator.CalculatePricing("StaffAugmentation", "Developer", months);

        using var doc = JsonDocument.Parse(result);
        var discount = doc.RootElement.GetProperty("volumeDiscount");

        Assert.Equal(expectedPercent, discount.GetProperty("percent").GetInt32());
        Assert.Equal(expectedLabel, discount.GetProperty("label").GetString());

        // Verify math: totalPrice = subtotal - discount amount
        var subtotal = doc.RootElement.GetProperty("subtotalBeforeDiscount").GetDecimal();
        var discountAmount = discount.GetProperty("amount").GetDecimal();
        var totalPrice = doc.RootElement.GetProperty("totalPrice").GetDecimal();
        Assert.Equal(subtotal - discountAmount, totalPrice);
        Assert.Equal(subtotal * expectedPercent / 100m, discountAmount);
    }

    [Fact]
    public void CalculatePricing_VolumeDiscount_18Month_StrategicTier()
    {
        // Globex-style engagement: 18 months = Strategic 12% discount
        var result = _calculator.CalculatePricing("FixedBid", "Architect,SeniorDev,SeniorDev", 18);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;

        var subtotal = root.GetProperty("subtotalBeforeDiscount").GetDecimal();
        var discount = root.GetProperty("volumeDiscount");
        Assert.Equal(12, discount.GetProperty("percent").GetInt32());
        Assert.Equal("Strategic", discount.GetProperty("label").GetString());
        Assert.Equal(subtotal * 0.12m, discount.GetProperty("amount").GetDecimal());
        Assert.Equal(subtotal * 0.88m, root.GetProperty("totalPrice").GetDecimal());
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "global.json")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new InvalidOperationException("Could not find repo root (looking for global.json)");
    }
}
