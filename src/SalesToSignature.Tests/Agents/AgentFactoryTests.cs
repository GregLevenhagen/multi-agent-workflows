using Microsoft.Extensions.AI;
using Moq;
using SalesToSignature.Agents.Agents;
using Xunit;

namespace SalesToSignature.Tests.Agents;

public class AgentFactoryTests
{
    private readonly IChatClient _mockClient = new Mock<IChatClient>().Object;

    [Fact]
    public void IAgentFactory_Validate_DefaultReturnsEmpty()
    {
        // Default interface method returns empty list (no errors)
        // Must be typed as IAgentFactory to call the default interface method
#pragma warning disable CA1859 // Intentionally using interface type to test default method
        IAgentFactory factory = new CoordinatorAgentFactory(_mockClient);
#pragma warning restore CA1859

        var errors = factory.Validate();

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(typeof(CoordinatorAgentFactory), "coordinator")]
    [InlineData(typeof(QualificationAgentFactory), "qualification")]
    [InlineData(typeof(ReviewAgentFactory), "review")]
    public void AgentFactory_WithoutTools_CreatesNamedAgent(Type factoryType, string expectedName)
    {
        var factory = (IAgentFactory)Activator.CreateInstance(factoryType, _mockClient, null)!;
        var agent = factory.Create();

        Assert.Equal(expectedName, agent.Name);
        Assert.False(string.IsNullOrEmpty(agent.Description));
    }

    [Fact]
    public void IntakeAgentFactory_CreatesAgentWithTools()
    {
        var tool = DocumentParser_CreateToolStub();
        var factory = new IntakeAgentFactory(_mockClient, [tool]);

        var agent = factory.Create();

        Assert.Equal("intake", agent.Name);
    }

    [Fact]
    public void ApprovalAgentFactory_CreatesAgentWithTools()
    {
        var tool = DocumentParser_CreateToolStub();
        var factory = new ApprovalAgentFactory(_mockClient, [tool]);

        var agent = factory.Create();

        Assert.Equal("approval", agent.Name);
    }

    [Fact]
    public void AllFactories_ImplementIAgentFactory()
    {
        var factoryTypes = new[]
        {
            typeof(CoordinatorAgentFactory),
            typeof(IntakeAgentFactory),
            typeof(QualificationAgentFactory),
            typeof(ProposalAgentFactory),
            typeof(ContractAgentFactory),
            typeof(ReviewAgentFactory),
            typeof(ApprovalAgentFactory)
        };

        foreach (var type in factoryTypes)
        {
            Assert.True(typeof(IAgentFactory).IsAssignableFrom(type),
                $"{type.Name} should implement IAgentFactory");
        }
    }

    private static AIFunction DocumentParser_CreateToolStub()
    {
        return SalesToSignature.Agents.Tools.DocumentParser.CreateTool();
    }
}
