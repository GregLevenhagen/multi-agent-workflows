using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace SalesToSignature.Agents.Tools;

public static partial class DocumentParser
{
    [Description("Parses and cleans raw document text by removing HTML tags, normalizing encoding artifacts and whitespace, and stripping non-printable characters. Returns cleaned text ready for structured extraction.")]
    public static AIFunction CreateTool()
    {
        return AIFunctionFactory.Create(ParseDocument);
    }

    [Description("Cleans and normalizes raw document text for structured data extraction.")]
    public static string ParseDocument(
        [Description("The raw document text to parse and clean")] string documentText)
    {
        if (string.IsNullOrWhiteSpace(documentText))
            return string.Empty;

        // Strip BOM if present
        var cleaned = documentText.TrimStart('\uFEFF');

        // Strip HTML tags (RFPs may be copy-pasted from web portals)
        cleaned = HtmlTagRegex().Replace(cleaned, "");

        // Decode common HTML entities
        cleaned = cleaned
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"")
            .Replace("&#39;", "'")
            .Replace("&nbsp;", " ");

        // Normalize common Unicode encoding artifacts to ASCII equivalents
        cleaned = cleaned
            .Replace('\u201C', '"')   // left double quotation mark
            .Replace('\u201D', '"')   // right double quotation mark
            .Replace('\u2018', '\'')  // left single quotation mark
            .Replace('\u2019', '\'')  // right single quotation mark
            .Replace('\u2014', '-')   // em dash
            .Replace('\u2013', '-')   // en dash
            .Replace('\u2026', '.')   // ellipsis → single dot (context preserved by surrounding text)
            .Replace('\u00A0', ' ');  // non-breaking space

        // Normalize line endings
        cleaned = cleaned.Replace("\r\n", "\n").Replace("\r", "\n");

        // Remove remaining non-printable characters except newlines and tabs
        cleaned = NonPrintableRegex().Replace(cleaned, "");

        // Collapse multiple blank lines into double newline
        cleaned = MultipleBlankLinesRegex().Replace(cleaned, "\n\n");

        // Trim leading/trailing whitespace from each line
        var lines = cleaned.Split('\n')
            .Select(line => line.Trim())
            .ToArray();

        cleaned = string.Join("\n", lines);

        // Trim overall
        return cleaned.Trim();
    }

    [GeneratedRegex(@"<[^>\r\n]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"[^\x20-\x7E\n\t]")]
    private static partial Regex NonPrintableRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex MultipleBlankLinesRegex();
}
