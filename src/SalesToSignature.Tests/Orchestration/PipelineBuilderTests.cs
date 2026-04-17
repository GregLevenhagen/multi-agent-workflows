using Microsoft.Extensions.AI;
using Moq;
using SalesToSignature.Agents.Orchestration;
using Xunit;

namespace SalesToSignature.Tests.Orchestration;

public class PipelineBuilderTests
{
    [Fact]
    public void BuildPipeline_WithMockedClient_ReturnsWorkflow()
    {
        var mockClient = new Mock<IChatClient>();
        var dataDir = Path.Combine(FindRepoRoot(), "data");

        var workflow = PipelineBuilder.BuildPipeline(mockClient.Object, dataDir);

        Assert.NotNull(workflow);
    }

    [Fact]
    public void BuildPipeline_WithDefaultDataDirectory_ReturnsWorkflow()
    {
        var mockClient = new Mock<IChatClient>();

        // null dataDirectory falls back to env var or relative path
        var workflow = PipelineBuilder.BuildPipeline(mockClient.Object);

        Assert.NotNull(workflow);
    }

    [Fact]
    public void GetAgentNames_Returns7AgentsInOrder()
    {
        var names = PipelineBuilder.GetAgentNames();

        Assert.Equal(7, names.Count);
        Assert.Equal("coordinator", names[0]);
        Assert.Equal("intake", names[1]);
        Assert.Equal("qualification", names[2]);
        Assert.Equal("proposal", names[3]);
        Assert.Equal("contract", names[4]);
        Assert.Equal("review", names[5]);
        Assert.Equal("approval", names[6]);
    }

    [Fact]
    public void ExplicitHandoffInstructions_ExplainToolBasedRouting()
    {
        var instructions = PipelineBuilder.ExplicitHandoffInstructions;

        Assert.Contains("handoff_to_", instructions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tool descriptions", instructions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("workflow should terminate", instructions, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildPipeline_WithLoggerFactory_DoesNotThrow()
    {
        var mockClient = new Mock<IChatClient>();
        var dataDir = Path.Combine(FindRepoRoot(), "data");

        var workflow = PipelineBuilder.BuildPipeline(mockClient.Object, dataDir, loggerFactory: null);

        Assert.NotNull(workflow);
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
