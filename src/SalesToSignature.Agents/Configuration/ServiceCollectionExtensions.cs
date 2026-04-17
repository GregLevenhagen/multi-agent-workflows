using Microsoft.Extensions.DependencyInjection;
using SalesToSignature.Agents.Tools;

namespace SalesToSignature.Agents.Configuration;

/// <summary>
/// Extension methods for registering pipeline services (tools, safety middleware, telemetry)
/// in the DI container. Keeps Program.cs focused on composition rather than registration details.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all pipeline tool instances as singletons using the specified data directory.
    /// Resolution chain for data directory: explicit parameter → DATA_DIRECTORY env var → relative path fallback.
    /// </summary>
    public static IServiceCollection AddPipelineTools(this IServiceCollection services, string? dataDirectory = null)
    {
        services.AddSingleton(_ => new TemplateLookup(dataDirectory));
        services.AddSingleton(_ => new PricingCalculator(dataDirectory));
        services.AddSingleton(_ => new LegalTemplateLookup(dataDirectory));
        services.AddSingleton(_ => new ClauseLibrary(dataDirectory));

        return services;
    }

    /// <summary>
    /// Validates that all registered tool instances can access their required data files.
    /// Returns a list of validation errors; empty list means all tools are ready.
    /// Call after building the service provider to verify data directory configuration.
    /// </summary>
    public static IReadOnlyList<string> ValidatePipelineTools(this IServiceProvider provider, string? dataDirectory = null)
    {
        var errors = new List<string>();

        var resolvedDir = dataDirectory
            ?? Environment.GetEnvironmentVariable("DATA_DIRECTORY")
            ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data");

        if (!Directory.Exists(resolvedDir))
        {
            errors.Add($"Data directory not found: {resolvedDir}");
            return errors;
        }

        var requiredFiles = new[]
        {
            Path.Combine(resolvedDir, "pricing", "rate-cards.json"),
            Path.Combine(resolvedDir, "legal", "standard-clauses.json"),
            Path.Combine(resolvedDir, "legal", "engagement-specific-clauses.json"),
            Path.Combine(resolvedDir, "legal", "msa-template.md"),
            Path.Combine(resolvedDir, "legal", "nda-template.md"),
            Path.Combine(resolvedDir, "templates", "sow-template-staffaug.md"),
            Path.Combine(resolvedDir, "templates", "sow-template-fixedbid.md"),
            Path.Combine(resolvedDir, "templates", "sow-template-tm.md"),
            Path.Combine(resolvedDir, "templates", "sow-template-advisory.md")
        };

        foreach (var file in requiredFiles)
        {
            if (!File.Exists(file))
            {
                errors.Add($"Required data file missing: {Path.GetFileName(file)}");
            }
        }

        return errors;
    }
}
