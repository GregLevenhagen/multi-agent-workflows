using SalesToSignature.Agents.Tools;
using Xunit;

namespace SalesToSignature.Tests.Tools;

public class ApproveContractTests
{
    [Fact]
    public void RequestApproval_ReturnsFormattedSummary()
    {
        var result = ApproveContract.RequestApproval(
            contractSummary: "12-month Azure cloud migration engagement",
            totalValue: 528000m,
            clientName: "Acme Corp",
            engagementType: "StaffAugmentation");

        Assert.Contains("Acme Corp", result);
        Assert.Contains("StaffAugmentation", result);
        Assert.Contains("$528,000", result);
        Assert.Contains("12-month Azure cloud migration engagement", result);
        Assert.Contains("APPROVAL REQUEST", result);
    }

    [Fact]
    public void RequestApproval_IncludesAllFields()
    {
        var result = ApproveContract.RequestApproval(
            contractSummary: "Fixed-bid data platform build",
            totalValue: 1200000m,
            clientName: "Globex International",
            engagementType: "FixedBid");

        Assert.Contains("Client:", result);
        Assert.Contains("Engagement:", result);
        Assert.Contains("Value:", result);
        Assert.Contains("Summary:", result);
    }

    [Fact]
    public void CreateTool_ReturnsAIFunction()
    {
        var tool = ApproveContract.CreateTool();

        Assert.NotNull(tool);
    }
}
