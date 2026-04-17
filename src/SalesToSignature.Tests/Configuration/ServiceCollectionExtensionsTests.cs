using Microsoft.Extensions.DependencyInjection;
using SalesToSignature.Agents.Configuration;
using SalesToSignature.Agents.Tools;
using Xunit;

namespace SalesToSignature.Tests.Configuration;

public class ServiceCollectionExtensionsTests
{
    private static string FindDataDir()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "global.json")))
                return Path.Combine(dir, "data");
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new InvalidOperationException("Could not find repo root (looking for global.json)");
    }

    [Fact]
    public void AddPipelineTools_RegistersAllToolInstances()
    {
        var services = new ServiceCollection();
        var dataDir = FindDataDir();

        services.AddPipelineTools(dataDir);

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<TemplateLookup>());
        Assert.NotNull(provider.GetService<PricingCalculator>());
        Assert.NotNull(provider.GetService<LegalTemplateLookup>());
        Assert.NotNull(provider.GetService<ClauseLibrary>());
    }

    [Fact]
    public void AddPipelineTools_RegistersAsSingleton()
    {
        var services = new ServiceCollection();
        var dataDir = FindDataDir();

        services.AddPipelineTools(dataDir);

        var provider = services.BuildServiceProvider();
        var first = provider.GetService<TemplateLookup>();
        var second = provider.GetService<TemplateLookup>();
        Assert.Same(first, second);
    }

    [Fact]
    public void ValidatePipelineTools_WithValidDataDir_ReturnsNoErrors()
    {
        var services = new ServiceCollection();
        var dataDir = FindDataDir();
        services.AddPipelineTools(dataDir);
        var provider = services.BuildServiceProvider();

        var errors = provider.ValidatePipelineTools(dataDir);

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidatePipelineTools_WithInvalidDataDir_ReturnsDirectoryError()
    {
        var services = new ServiceCollection();
        services.AddPipelineTools("/nonexistent/path");
        var provider = services.BuildServiceProvider();

        var errors = provider.ValidatePipelineTools("/nonexistent/path");

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Data directory not found"));
    }
}
