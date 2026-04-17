using System.Collections.Concurrent;
using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace SalesToSignature.Agents.Tools;

public class TemplateLookup
{
    private static readonly Dictionary<string, string> EngagementTypeToFile = new(StringComparer.OrdinalIgnoreCase)
    {
        ["StaffAugmentation"] = "sow-template-staffaug.md",
        ["FixedBid"] = "sow-template-fixedbid.md",
        ["TimeAndMaterials"] = "sow-template-tm.md",
        ["Advisory"] = "sow-template-advisory.md"
    };

    private readonly ConcurrentDictionary<string, string> _templateCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _dataDirectory;

    public TemplateLookup(string? dataDirectory = null)
    {
        _dataDirectory = dataDirectory
            ?? Environment.GetEnvironmentVariable("DATA_DIRECTORY")
            ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data");
    }

    [Description("Looks up a Statement of Work (SOW) template for the given engagement type. Returns the template content with placeholder variables.")]
    public AIFunction CreateTool()
    {
        return AIFunctionFactory.Create(LookupTemplate);
    }

    [Description("Retrieves the SOW template for the specified engagement type.")]
    public string LookupTemplate(
        [Description("The engagement type: StaffAugmentation, FixedBid, TimeAndMaterials, or Advisory")] string engagementType)
    {
        if (!EngagementTypeToFile.TryGetValue(engagementType, out var fileName))
            return $"Error: Unknown engagement type '{engagementType}'. Valid types: StaffAugmentation, FixedBid, TimeAndMaterials, Advisory";

        return _templateCache.GetOrAdd(engagementType, _ =>
        {
            var templatePath = Path.Combine(_dataDirectory, "templates", fileName);

            if (!File.Exists(templatePath))
                return $"Error: Template file not found at {templatePath}";

            return File.ReadAllText(templatePath);
        });
    }
}
