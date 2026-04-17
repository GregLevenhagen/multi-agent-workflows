using SalesToSignature.Agents.Tools;
using Xunit;

namespace SalesToSignature.Tests.Tools;

public class LegalTemplateLookupTests
{
    private readonly LegalTemplateLookup _lookup;

    public LegalTemplateLookupTests()
    {
        var dataDir = Path.Combine(FindRepoRoot(), "data");
        _lookup = new LegalTemplateLookup(dataDir);
    }

    [Theory]
    [InlineData("msa", "Master Services Agreement")]
    [InlineData("nda", "Mutual Non-Disclosure Agreement")]
    public void LookupTemplate_ValidType_ReturnsTemplateWithTitle(string templateType, string expectedTitle)
    {
        var result = _lookup.LookupTemplate(templateType);

        Assert.Contains(expectedTitle, result);
    }

    [Theory]
    [InlineData("msa")]
    [InlineData("nda")]
    public void LookupTemplate_ValidType_ContainsPlaceholders(string templateType)
    {
        var result = _lookup.LookupTemplate(templateType);

        Assert.Contains("{{EFFECTIVE_DATE}}", result);
        Assert.Contains("{{CLIENT_NAME}}", result);
        Assert.Contains("{{CLIENT_ADDRESS}}", result);
    }

    [Theory]
    [InlineData("MSA")]
    [InlineData("Msa")]
    [InlineData("NDA")]
    [InlineData("Nda")]
    public void LookupTemplate_CaseInsensitive_ReturnsTemplate(string templateType)
    {
        var result = _lookup.LookupTemplate(templateType);

        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public void LookupTemplate_UnknownType_ReturnsError()
    {
        var result = _lookup.LookupTemplate("sow");

        Assert.StartsWith("Error: Unknown template type", result);
        Assert.Contains("sow", result);
    }

    [Fact]
    public void LookupTemplate_MissingFile_ReturnsFileNotFoundError()
    {
        var lookup = new LegalTemplateLookup("/nonexistent/path");
        var result = lookup.LookupTemplate("msa");

        Assert.StartsWith("Error: Template file not found", result);
    }

    [Fact]
    public void LookupTemplate_Msa_ContainsLegalSections()
    {
        var result = _lookup.LookupTemplate("msa");

        Assert.Contains("Intellectual Property", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Liability", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Confidentiality", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Termination", result, StringComparison.OrdinalIgnoreCase);
    }

    // --- Cache behavior tests ---

    [Fact]
    public void LookupTemplate_CalledTwice_ReturnsSameResult()
    {
        var first = _lookup.LookupTemplate("msa");
        var second = _lookup.LookupTemplate("msa");

        Assert.Equal(first, second);
        Assert.Same(first, second); // ConcurrentDictionary returns same string reference
    }

    // --- Placeholder validation tests ---

    [Theory]
    [InlineData("msa")]
    [InlineData("nda")]
    public void ValidatePlaceholders_RealTemplate_AllPresent(string templateType)
    {
        var template = _lookup.LookupTemplate(templateType);

        var missing = LegalTemplateLookup.ValidatePlaceholders(template);

        Assert.Empty(missing);
    }

    [Fact]
    public void ValidatePlaceholders_MissingPlaceholder_ReturnsIt()
    {
        var template = "This template has {{EFFECTIVE_DATE}} but nothing else.";

        var missing = LegalTemplateLookup.ValidatePlaceholders(template);

        Assert.Contains("{{CLIENT_NAME}}", missing);
        Assert.Contains("{{CLIENT_ADDRESS}}", missing);
        Assert.DoesNotContain("{{EFFECTIVE_DATE}}", missing);
    }

    [Fact]
    public void ExtractPlaceholders_ReturnsAllPlaceholders()
    {
        var template = "Hello {{CLIENT_NAME}}, effective {{EFFECTIVE_DATE}} at {{CLIENT_ADDRESS}}.";

        var placeholders = LegalTemplateLookup.ExtractPlaceholders(template);

        Assert.Equal(3, placeholders.Count);
        Assert.Contains("{{CLIENT_ADDRESS}}", placeholders);
        Assert.Contains("{{CLIENT_NAME}}", placeholders);
        Assert.Contains("{{EFFECTIVE_DATE}}", placeholders);
    }

    [Theory]
    [InlineData("msa")]
    [InlineData("nda")]
    public void ExtractPlaceholders_RealTemplate_HasExpectedPlaceholders(string templateType)
    {
        var template = _lookup.LookupTemplate(templateType);

        var placeholders = LegalTemplateLookup.ExtractPlaceholders(template);

        Assert.Contains("{{CLIENT_NAME}}", placeholders);
        Assert.Contains("{{EFFECTIVE_DATE}}", placeholders);
        Assert.Contains("{{CLIENT_ADDRESS}}", placeholders);
        Assert.True(placeholders.Count >= 3, $"Expected at least 3 placeholders, got {placeholders.Count}");
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
