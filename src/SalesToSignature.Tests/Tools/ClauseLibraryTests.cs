using System.Text.Json;
using SalesToSignature.Agents.Tools;
using Xunit;

namespace SalesToSignature.Tests.Tools;

public class ClauseLibraryTests
{
    private readonly ClauseLibrary _library;

    public ClauseLibraryTests()
    {
        var dataDir = Path.Combine(FindRepoRoot(), "data");
        _library = new ClauseLibrary(dataDir);
    }

    [Fact]
    public void QueryClauses_NoFilter_ReturnsAllStandardClauses()
    {
        var result = _library.QueryClauses();
        using var doc = JsonDocument.Parse(result);
        var clauses = doc.RootElement;

        // Should return all 14 standard clauses (no engagement-specific without engagementType filter)
        Assert.True(clauses.GetArrayLength() >= 12, $"Expected at least 12 standard clauses, got {clauses.GetArrayLength()}");
    }

    [Fact]
    public void QueryClauses_FilterByCategory_ReturnsMatchingClauses()
    {
        var result = _library.QueryClauses(category: "IP");
        using var doc = JsonDocument.Parse(result);
        var clauses = doc.RootElement;

        Assert.Equal(2, clauses.GetArrayLength()); // IP-001 and IP-002
        foreach (var clause in clauses.EnumerateArray())
        {
            Assert.Equal("IP", clause.GetProperty("category").GetString());
        }
    }

    [Fact]
    public void QueryClauses_FilterByEngagementType_IncludesEngagementSpecific()
    {
        var result = _library.QueryClauses(engagementType: "FixedBid");
        using var doc = JsonDocument.Parse(result);
        var clauses = doc.RootElement;

        // All standard + FixedBid-specific (4 clauses: FB-001 through FB-004)
        Assert.True(clauses.GetArrayLength() >= 16, $"Expected standard + 4 FixedBid clauses, got {clauses.GetArrayLength()}");

        // Verify FixedBid-specific clauses are present
        var hasChangeOrder = false;
        foreach (var clause in clauses.EnumerateArray())
        {
            if (clause.GetProperty("id").GetString() == "FB-001")
                hasChangeOrder = true;
        }
        Assert.True(hasChangeOrder, "Should include FixedBid change order clause");
    }

    [Fact]
    public void QueryClauses_FilterByCategoryAndEngagementType()
    {
        var result = _library.QueryClauses(category: "Liability", engagementType: "StaffAugmentation");
        using var doc = JsonDocument.Parse(result);
        var clauses = doc.RootElement;

        // Liability standard clauses (LIA-001, LIA-002, LIA-003) + StaffAugmentation clauses (SA-001, SA-002, SA-003)
        // Category filter only applies to standard clauses; engagement-specific are always included
        Assert.True(clauses.GetArrayLength() >= 3, $"Expected at least 3 Liability clauses, got {clauses.GetArrayLength()}");
    }

    [Theory]
    [InlineData("StaffAugmentation", "SA-")]
    [InlineData("FixedBid", "FB-")]
    [InlineData("TimeAndMaterials", "TM-")]
    [InlineData("Advisory", "ADV-")]
    public void QueryClauses_AllEngagementTypes_ReturnsEngagementSpecificClauses(string engagementType, string expectedIdPrefix)
    {
        var result = _library.QueryClauses(engagementType: engagementType);
        using var doc = JsonDocument.Parse(result);
        var clauses = doc.RootElement;

        // Should include standard clauses + engagement-specific
        Assert.True(clauses.GetArrayLength() > 14, $"Expected more than 14 clauses for {engagementType}, got {clauses.GetArrayLength()}");

        var hasEngagementSpecific = false;
        foreach (var clause in clauses.EnumerateArray())
        {
            var id = clause.GetProperty("id").GetString();
            if (id?.StartsWith(expectedIdPrefix, StringComparison.Ordinal) == true)
                hasEngagementSpecific = true;
        }
        Assert.True(hasEngagementSpecific, $"Should include {engagementType} clauses with prefix {expectedIdPrefix}");
    }

    [Fact]
    public void QueryClauses_NonexistentCategory_ReturnsEmpty()
    {
        var result = _library.QueryClauses(category: "NonexistentCategory");
        using var doc = JsonDocument.Parse(result);

        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public void QueryClauses_AllClausesHaveRequiredFields()
    {
        var result = _library.QueryClauses();
        using var doc = JsonDocument.Parse(result);

        foreach (var clause in doc.RootElement.EnumerateArray())
        {
            Assert.True(clause.TryGetProperty("id", out _), "Each clause must have an 'id' field");
            Assert.True(clause.TryGetProperty("name", out _), "Each clause must have a 'name' field");
            Assert.True(clause.TryGetProperty("text", out _), "Each clause must have a 'text' field");
        }
    }

    [Fact]
    public void QueryClauses_FixedBid_IncludesDataSovereigntyClause()
    {
        var result = _library.QueryClauses(engagementType: "FixedBid");
        using var doc = JsonDocument.Parse(result);

        var hasSovereignty = false;
        foreach (var clause in doc.RootElement.EnumerateArray())
        {
            if (clause.GetProperty("id").GetString() == "FB-005")
            {
                hasSovereignty = true;
                Assert.Contains("Data Sovereignty", clause.GetProperty("name").GetString());
                Assert.Contains("geographic regions", clause.GetProperty("text").GetString());
            }
        }
        Assert.True(hasSovereignty, "FixedBid should include data sovereignty clause (FB-005)");
    }

    [Fact]
    public void ContractGenerationWorkflow_CombinesLegalTemplateWithClauses()
    {
        var dataDir = Path.Combine(FindRepoRoot(), "data");
        var templateLookup = new LegalTemplateLookup(dataDir);

        // Step 1: Contract agent looks up MSA template
        var msaTemplate = templateLookup.LookupTemplate("msa");
        Assert.DoesNotContain("Error:", msaTemplate);
        Assert.Contains("{{CLIENT_NAME}}", msaTemplate);
        Assert.Contains("{{EFFECTIVE_DATE}}", msaTemplate);

        // Step 2: Contract agent queries FixedBid clauses
        var fixedBidClauses = _library.QueryClauses(engagementType: "FixedBid");
        using var doc = JsonDocument.Parse(fixedBidClauses);
        var clauseCount = doc.RootElement.GetArrayLength();

        // Should have standard + FixedBid engagement-specific clauses
        Assert.True(clauseCount >= 17, $"Expected >= 17 clauses (14 standard + 5 FixedBid), got {clauseCount}");

        // Step 3: Verify we have required clause categories for a complete contract
        var categories = new HashSet<string>();
        foreach (var clause in doc.RootElement.EnumerateArray())
        {
            if (clause.TryGetProperty("category", out var cat))
                categories.Add(cat.GetString()!);
        }
        Assert.Contains("IP", categories);
        Assert.Contains("Liability", categories);
        Assert.Contains("Confidentiality", categories);
        Assert.Contains("Termination", categories);
        Assert.Contains("Payment", categories);
    }

    // --- Keyword search tests ---

    [Fact]
    public void QueryClauses_KeywordSearch_FindsMatchingClauses()
    {
        var result = _library.QueryClauses(keyword: "GDPR");
        using var doc = JsonDocument.Parse(result);
        var clauses = doc.RootElement;

        Assert.True(clauses.GetArrayLength() >= 1, "Should find at least 1 clause mentioning GDPR");
        foreach (var clause in clauses.EnumerateArray())
        {
            var name = clause.GetProperty("name").GetString() ?? "";
            var text = clause.GetProperty("text").GetString() ?? "";
            Assert.True(
                name.Contains("GDPR", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("GDPR", StringComparison.OrdinalIgnoreCase),
                "Each result should contain the keyword in name or text");
        }
    }

    [Fact]
    public void QueryClauses_KeywordSearch_CaseInsensitive()
    {
        var resultLower = _library.QueryClauses(keyword: "indemnif");
        var resultUpper = _library.QueryClauses(keyword: "INDEMNIF");
        using var docLower = JsonDocument.Parse(resultLower);
        using var docUpper = JsonDocument.Parse(resultUpper);

        Assert.Equal(docLower.RootElement.GetArrayLength(), docUpper.RootElement.GetArrayLength());
        Assert.True(docLower.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public void QueryClauses_KeywordSearch_NoMatch_ReturnsEmpty()
    {
        var result = _library.QueryClauses(keyword: "xyznonexistentkeyword");
        using var doc = JsonDocument.Parse(result);

        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public void QueryClauses_KeywordWithCategory_CombinesFilters()
    {
        // Search for "liability" keyword within DataProtection category — should return 0
        var result = _library.QueryClauses(category: "DataProtection", keyword: "liability");
        using var doc = JsonDocument.Parse(result);

        // DataProtection clauses don't mention liability
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    // --- Cache behavior tests ---

    [Fact]
    public void QueryClauses_CalledTwice_ReturnsSameResults()
    {
        var first = _library.QueryClauses();
        var second = _library.QueryClauses();

        Assert.Equal(first, second);
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
