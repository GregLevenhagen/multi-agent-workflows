using SalesToSignature.Agents.Tools;
using Xunit;

namespace SalesToSignature.Tests.Tools;

public class DocumentParserTests
{
    [Fact]
    public void ParseDocument_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, DocumentParser.ParseDocument(""));
        Assert.Equal(string.Empty, DocumentParser.ParseDocument("   "));
    }

    [Fact]
    public void ParseDocument_NormalizesLineEndings()
    {
        var input = "Line 1\r\nLine 2\rLine 3\nLine 4";
        var result = DocumentParser.ParseDocument(input);

        Assert.DoesNotContain("\r", result);
        Assert.Contains("Line 1\nLine 2\nLine 3\nLine 4", result);
    }

    [Fact]
    public void ParseDocument_CollapsesMultipleBlankLines()
    {
        var input = "Section 1\n\n\n\n\nSection 2";
        var result = DocumentParser.ParseDocument(input);

        Assert.Equal("Section 1\n\nSection 2", result);
    }

    [Fact]
    public void ParseDocument_TrimsWhitespaceFromLines()
    {
        var input = "  Leading spaces  \n  Trailing spaces  ";
        var result = DocumentParser.ParseDocument(input);

        Assert.Equal("Leading spaces\nTrailing spaces", result);
    }

    [Fact]
    public void ParseDocument_StripsHtmlTags()
    {
        var input = "<p>This is a <strong>bold</strong> paragraph.</p>";
        var result = DocumentParser.ParseDocument(input);

        Assert.Equal("This is a bold paragraph.", result);
    }

    [Fact]
    public void ParseDocument_DecodesHtmlEntities()
    {
        var input = "Tom &amp; Jerry &lt;2024&gt; said &quot;hello&quot;";
        var result = DocumentParser.ParseDocument(input);

        Assert.Equal("Tom & Jerry <2024> said \"hello\"", result);
    }

    [Fact]
    public void ParseDocument_NormalizesSmartQuotes()
    {
        var input = "\u201CDouble quoted\u201D and \u2018single quoted\u2019";
        var result = DocumentParser.ParseDocument(input);

        Assert.Equal("\"Double quoted\" and 'single quoted'", result);
    }

    [Fact]
    public void ParseDocument_NormalizesDashes()
    {
        var input = "Em dash\u2014here and en dash\u2013there";
        var result = DocumentParser.ParseDocument(input);

        Assert.Equal("Em dash-here and en dash-there", result);
    }

    [Fact]
    public void ParseDocument_NormalizesEllipsisAndNbsp()
    {
        var input = "Wait for it\u2026\u00A0done";
        var result = DocumentParser.ParseDocument(input);

        Assert.Equal("Wait for it. done", result);
    }

    [Fact]
    public void ParseDocument_StripsBom()
    {
        var input = "\uFEFFDocument with BOM";
        var result = DocumentParser.ParseDocument(input);

        Assert.Equal("Document with BOM", result);
    }

    [Fact]
    public void ParseDocument_PreservesLessThanComparisons()
    {
        var input = "Latency <2 seconds\nBudget: $1,200,000\nRPO <15 minutes";
        var result = DocumentParser.ParseDocument(input);

        Assert.Contains("<2 seconds", result);
        Assert.Contains("$1,200,000", result);
        Assert.Contains("<15 minutes", result);
    }

    [Fact]
    public void ParseDocument_ComplexHtmlDocument()
    {
        var input = """
            <html><head><title>RFP</title></head>
            <body>
            <h1>Request for Proposal</h1>
            <p>Client: Acme &amp; Co.</p>
            <ul>
            <li>Budget: $500K&ndash;$750K</li>
            <li>Timeline: 12 months</li>
            </ul>
            </body></html>
            """;
        var result = DocumentParser.ParseDocument(input);

        Assert.Contains("Request for Proposal", result);
        Assert.Contains("Client: Acme & Co.", result);
        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain(">", result);
    }

    [Fact]
    public void ParseDocument_RealRfpFile_PreservesContent()
    {
        var rfpPath = Path.Combine(FindRepoRoot(), "data", "rfps", "acme-cloud-migration-rfp.md");
        var rawText = File.ReadAllText(rfpPath);
        var result = DocumentParser.ParseDocument(rawText);

        Assert.Contains("Acme", result);
        Assert.Contains("cloud", result, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.Length > 100, "Parsed RFP should retain substantial content");
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
