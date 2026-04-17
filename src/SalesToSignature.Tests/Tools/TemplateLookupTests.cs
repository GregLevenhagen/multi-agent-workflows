using SalesToSignature.Agents.Tools;
using Xunit;

namespace SalesToSignature.Tests.Tools;

public class TemplateLookupTests
{
    private readonly TemplateLookup _lookup;

    public TemplateLookupTests()
    {
        var dataDir = Path.Combine(FindRepoRoot(), "data");
        _lookup = new TemplateLookup(dataDir);
    }

    [Theory]
    [InlineData("StaffAugmentation", "Staff Augmentation Services")]
    [InlineData("FixedBid", "Fixed-Bid Engagement")]
    [InlineData("TimeAndMaterials", "Time and Materials Engagement")]
    [InlineData("Advisory", "Advisory Engagement")]
    public void LookupTemplate_ValidType_ReturnsTemplateWithTitle(string engagementType, string expectedTitleFragment)
    {
        var result = _lookup.LookupTemplate(engagementType);

        Assert.Contains(expectedTitleFragment, result);
    }

    [Theory]
    [InlineData("StaffAugmentation")]
    [InlineData("FixedBid")]
    [InlineData("TimeAndMaterials")]
    [InlineData("Advisory")]
    public void LookupTemplate_ValidType_ContainsPlaceholders(string engagementType)
    {
        var result = _lookup.LookupTemplate(engagementType);

        Assert.Contains("{{CLIENT_NAME}}", result);
        Assert.Contains("{{ENGAGEMENT_ID}}", result);
        Assert.Contains("{{EFFECTIVE_DATE}}", result);
    }

    [Fact]
    public void LookupTemplate_UnknownType_ReturnsError()
    {
        var result = _lookup.LookupTemplate("InvalidType");

        Assert.StartsWith("Error: Unknown engagement type", result);
        Assert.Contains("InvalidType", result);
    }

    [Fact]
    public void LookupTemplate_MissingFile_ReturnsFileNotFoundError()
    {
        var lookup = new TemplateLookup("/nonexistent/path");
        var result = lookup.LookupTemplate("StaffAugmentation");

        Assert.StartsWith("Error: Template file not found", result);
    }

    [Theory]
    [InlineData("staffaugmentation")]
    [InlineData("STAFFAUGMENTATION")]
    [InlineData("staffAugmentation")]
    public void LookupTemplate_CaseInsensitive_ReturnsTemplate(string engagementType)
    {
        var result = _lookup.LookupTemplate(engagementType);

        Assert.Contains("Staff Augmentation Services", result);
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
