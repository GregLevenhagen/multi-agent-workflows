using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace SalesToSignature.Agents.Tools;

public partial class LegalTemplateLookup
{
    private static readonly Dictionary<string, string> TemplateTypeToFile = new(StringComparer.OrdinalIgnoreCase)
    {
        ["msa"] = "msa-template.md",
        ["nda"] = "nda-template.md"
    };

    private readonly ConcurrentDictionary<string, string> _templateCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _dataDirectory;

    public LegalTemplateLookup(string? dataDirectory = null)
    {
        _dataDirectory = dataDirectory
            ?? Environment.GetEnvironmentVariable("DATA_DIRECTORY")
            ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data");
    }

    [Description("Looks up a legal template (MSA or NDA) and returns its content with placeholder variables.")]
    public AIFunction CreateTool()
    {
        return AIFunctionFactory.Create(LookupTemplate);
    }

    /// <summary>Required placeholders that every legal template must contain.</summary>
    private static readonly string[] RequiredPlaceholders =
        ["{{EFFECTIVE_DATE}}", "{{CLIENT_NAME}}", "{{CLIENT_ADDRESS}}"];

    private static readonly Regex PlaceholderPattern = PlaceholderRegex();

    [Description("Retrieves a legal template by type.")]
    public string LookupTemplate(
        [Description("The template type: 'msa' for Master Services Agreement or 'nda' for Non-Disclosure Agreement")] string templateType)
    {
        if (!TemplateTypeToFile.TryGetValue(templateType, out var fileName))
            return $"Error: Unknown template type '{templateType}'. Valid types: msa, nda";

        return _templateCache.GetOrAdd(templateType, _ =>
        {
            var templatePath = Path.Combine(_dataDirectory, "legal", fileName);

            if (!File.Exists(templatePath))
                return $"Error: Template file not found at {templatePath}";

            return File.ReadAllText(templatePath);
        });
    }

    /// <summary>
    /// Validates that a template contains all required placeholders.
    /// Returns a list of missing placeholder names; empty list means the template is valid.
    /// </summary>
    public static IReadOnlyList<string> ValidatePlaceholders(string templateContent)
    {
        return RequiredPlaceholders
            .Where(p => !templateContent.Contains(p, StringComparison.Ordinal))
            .ToList();
    }

    /// <summary>
    /// Extracts all placeholder names from a template (matches {{PLACEHOLDER}} pattern).
    /// </summary>
    public static IReadOnlyList<string> ExtractPlaceholders(string templateContent)
    {
        return PlaceholderPattern.Matches(templateContent)
            .Select(m => m.Value)
            .Distinct()
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
    }

    [GeneratedRegex(@"\{\{[A-Z_]+\}\}")]
    private static partial Regex PlaceholderRegex();
}
