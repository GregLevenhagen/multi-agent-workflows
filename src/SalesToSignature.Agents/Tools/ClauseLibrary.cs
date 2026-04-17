using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;
using SalesToSignature.Agents.Models;

namespace SalesToSignature.Agents.Tools;

public class ClauseLibrary
{
    private List<JsonElement>? _standardClausesCache;
    private Dictionary<string, List<JsonElement>>? _engagementClausesCache;
    private readonly string _dataDirectory;

    public ClauseLibrary(string? dataDirectory = null)
    {
        _dataDirectory = dataDirectory
            ?? Environment.GetEnvironmentVariable("DATA_DIRECTORY")
            ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data");
    }

    [Description("Queries the clause library for standard and engagement-specific contract clauses. Can filter by category, engagement type, or both.")]
    public AIFunction CreateTool()
    {
        return AIFunctionFactory.Create(QueryClauses);
    }

    [Description("Retrieves contract clauses filtered by category, engagement type, and/or keyword search.")]
    public string QueryClauses(
        [Description("Optional clause category filter: IP, Liability, Termination, Payment, Confidentiality, DataProtection, ForceMajeure, NonSolicitation")] string? category = null,
        [Description("Optional engagement type filter: StaffAugmentation, FixedBid, TimeAndMaterials, Advisory")] string? engagementType = null,
        [Description("Optional keyword to search in clause name and text (case-insensitive)")] string? keyword = null)
    {
        var results = new List<JsonElement>();

        // Load standard clauses (cached after first read)
        var standardClauses = LoadStandardClauses();
        foreach (var clause in standardClauses)
        {
            if (category != null)
            {
                var clauseCategory = clause.GetProperty("category").GetString();
                if (!string.Equals(clauseCategory, category, StringComparison.OrdinalIgnoreCase))
                    continue;
            }
            results.Add(clause);
        }

        // Load engagement-specific clauses (cached after first read)
        if (engagementType != null)
        {
            var engagementClauses = LoadEngagementClauses();
            if (engagementClauses.TryGetValue(engagementType, out var clauses))
            {
                results.AddRange(clauses);
            }
        }

        // Apply keyword search across name and text fields
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            results = results.Where(c =>
                (c.GetProperty("name").GetString()?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true) ||
                (c.GetProperty("text").GetString()?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true)
            ).ToList();
        }

        return JsonSerializer.Serialize(results, PipelineJsonOptions.Indented);
    }

    private List<JsonElement> LoadStandardClauses()
    {
        if (_standardClausesCache != null)
            return _standardClausesCache;

        var standardPath = Path.Combine(_dataDirectory, "legal", "standard-clauses.json");
        if (!File.Exists(standardPath))
            return _standardClausesCache = [];

        var standardJson = File.ReadAllText(standardPath);
        using var standardDoc = JsonDocument.Parse(standardJson);
        _standardClausesCache = standardDoc.RootElement.EnumerateArray().Select(c => c.Clone()).ToList();
        return _standardClausesCache;
    }

    private Dictionary<string, List<JsonElement>> LoadEngagementClauses()
    {
        if (_engagementClausesCache != null)
            return _engagementClausesCache;

        _engagementClausesCache = new Dictionary<string, List<JsonElement>>(StringComparer.OrdinalIgnoreCase);
        var engagementPath = Path.Combine(_dataDirectory, "legal", "engagement-specific-clauses.json");
        if (!File.Exists(engagementPath))
            return _engagementClausesCache;

        var engagementJson = File.ReadAllText(engagementPath);
        using var engagementDoc = JsonDocument.Parse(engagementJson);
        foreach (var prop in engagementDoc.RootElement.EnumerateObject())
        {
            _engagementClausesCache[prop.Name] = prop.Value.EnumerateArray().Select(c => c.Clone()).ToList();
        }
        return _engagementClausesCache;
    }
}
