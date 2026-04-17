using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SalesToSignature.Agents.Telemetry;

namespace SalesToSignature.Agents.Agents;

/// <summary>
/// Factory for the Review agent. Cross-checks contract and proposal against the original RFP.
/// Routes issues back to Contract or Proposal agents, or forwards to Approval if clean.
/// </summary>
public class ReviewAgentFactory(IChatClient chatClient, ILoggerFactory? loggerFactory = null) : IAgentFactory
{
    public ChatClientAgent Create()
    {
        using var activity = TelemetrySetup.AgentActivitySource.StartActivity("create_agent review");

        var agent = new ChatClientAgent(
            chatClient,
            AgentInstructions.ReviewInstructions,
            name: "review",
            description: "Cross-checks contract and proposal against the original RFP for consistency.");

        activity?.SetTag("gen_ai.operation.name", "create_agent");
        activity?.SetTag("gen_ai.system", "openai");
        activity?.SetTag("gen_ai.provider.name", "azure.ai.inference");
        activity?.SetTag("gen_ai.agent.name", agent.Name);
        activity?.SetTag("gen_ai.agent.description", agent.Description ?? "");
        activity?.SetTag("agent.name", agent.Name);
        activity?.SetTag("agent.description", agent.Description ?? "");

        loggerFactory?.CreateLogger<ReviewAgentFactory>()
            .LogDebug("Created agent '{AgentName}'", agent.Name);

        return agent;
    }
}
