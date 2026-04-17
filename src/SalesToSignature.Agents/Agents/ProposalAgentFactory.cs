using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SalesToSignature.Agents.Telemetry;

namespace SalesToSignature.Agents.Agents;

/// <summary>
/// Factory for the Proposal agent. Drafts SOW using TemplateLookup and PricingCalculator tools.
/// </summary>
public class ProposalAgentFactory(IChatClient chatClient, IList<AITool> tools, ILoggerFactory? loggerFactory = null) : IAgentFactory
{
    public ChatClientAgent Create()
    {
        using var activity = TelemetrySetup.AgentActivitySource.StartActivity("create_agent proposal");

        var agent = new ChatClientAgent(
            chatClient,
            AgentInstructions.ProposalInstructions,
            name: "proposal",
            description: "Drafts Statement of Work proposals using templates and pricing calculator.",
            tools: tools);

        activity?.SetTag("gen_ai.operation.name", "create_agent");
        activity?.SetTag("gen_ai.system", "openai");
        activity?.SetTag("gen_ai.provider.name", "azure.ai.inference");
        activity?.SetTag("gen_ai.agent.name", agent.Name);
        activity?.SetTag("gen_ai.agent.description", agent.Description ?? "");
        activity?.SetTag("agent.name", agent.Name);
        activity?.SetTag("agent.tool_count", tools.Count);
        activity?.SetTag("agent.description", agent.Description ?? "");

        loggerFactory?.CreateLogger<ProposalAgentFactory>()
            .LogDebug("Created agent '{AgentName}' with {ToolCount} tool(s)", agent.Name, tools.Count);

        return agent;
    }
}
