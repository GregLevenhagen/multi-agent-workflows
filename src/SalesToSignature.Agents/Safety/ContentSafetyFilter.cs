using System.Diagnostics;
using Azure.AI.ContentSafety;
using SalesToSignature.Agents.Configuration;

namespace SalesToSignature.Agents.Safety;

public record SafetyResult(bool IsBlocked, Dictionary<string, int> Categories);

public class ContentSafetyFilter
{
    private readonly ContentSafetyClient _client;
    private readonly int _severityThreshold;
    private static readonly ActivitySource ActivitySource = new("SalesToSignature.Agents");

    public ContentSafetyFilter(ContentSafetyClient client, AgentSettings? settings = null)
    {
        _client = client;
        _severityThreshold = settings?.ContentSafetySeverityThreshold ?? 4;
    }

    /// <summary>
    /// Analyzes text across all 4 content safety categories (Hate, SelfHarm, Sexual, Violence).
    /// Returns whether the text should be blocked based on severity thresholds.
    /// </summary>
    public virtual async Task<SafetyResult> AnalyzeTextAsync(string text, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("ContentSafety.AnalyzeText");

        try
        {
            var options = new AnalyzeTextOptions(text);
            var response = await _client.AnalyzeTextAsync(options, cancellationToken);

            var categories = new Dictionary<string, int>();
            var isBlocked = false;

            foreach (var analysis in response.Value.CategoriesAnalysis)
            {
                var severity = analysis.Severity ?? 0;
                categories[analysis.Category.ToString()] = severity;

                if (severity >= _severityThreshold)
                    isBlocked = true;
            }

            var result = new SafetyResult(isBlocked, categories);

            activity?.SetTag("content_safety.blocked", result.IsBlocked);
            foreach (var (category, severity) in result.Categories)
            {
                activity?.SetTag($"content_safety.{category.ToLowerInvariant()}", severity);
            }

            return result;
        }
        catch (Azure.RequestFailedException)
        {
            activity?.SetTag("content_safety.fallback", true);
            return new SafetyResult(false, new Dictionary<string, int>());
        }
    }

    /// <summary>
    /// Analyzes a long text by splitting it into chunks that fit within the API character limit.
    /// Returns blocked=true if ANY chunk is blocked, with the highest severity per category across all chunks.
    /// </summary>
    public virtual async Task<SafetyResult> AnalyzeLongTextAsync(
        string text,
        int maxChunkSize = 10_000,
        CancellationToken cancellationToken = default)
    {
        if (text.Length <= maxChunkSize)
            return await AnalyzeTextAsync(text, cancellationToken);

        var aggregatedCategories = new Dictionary<string, int>();
        var isBlocked = false;

        for (var offset = 0; offset < text.Length; offset += maxChunkSize)
        {
            var chunk = text.Substring(offset, Math.Min(maxChunkSize, text.Length - offset));
            var result = await AnalyzeTextAsync(chunk, cancellationToken);

            if (result.IsBlocked)
                isBlocked = true;

            foreach (var (category, severity) in result.Categories)
            {
                if (!aggregatedCategories.TryGetValue(category, out var existing) || severity > existing)
                    aggregatedCategories[category] = severity;
            }
        }

        return new SafetyResult(isBlocked, aggregatedCategories);
    }
}
