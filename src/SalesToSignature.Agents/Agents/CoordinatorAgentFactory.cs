using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SalesToSignature.Agents.Telemetry;

namespace SalesToSignature.Agents.Agents;

/// <summary>
/// Factory for the Coordinator agent. Routes incoming RFPs to Intake and reports no-go decisions.
/// </summary>
public class CoordinatorAgentFactory(IChatClient chatClient, ILoggerFactory? loggerFactory = null) : IAgentFactory
{
    public ChatClientAgent Create()
    {
        using var activity = TelemetrySetup.AgentActivitySource.StartActivity("create_agent coordinator");

        var agent = new ChatClientAgent(
            chatClient,
            AgentInstructions.CoordinatorInstructions,
            name: "coordinator",
            description: "Routes incoming RFP documents to the appropriate agent and reports final decisions.");

        activity?.SetTag("gen_ai.operation.name", "create_agent");
        activity?.SetTag("gen_ai.system", "openai");
        activity?.SetTag("gen_ai.provider.name", "azure.ai.inference");
        activity?.SetTag("gen_ai.agent.name", agent.Name);
        activity?.SetTag("gen_ai.agent.description", agent.Description ?? "");
        activity?.SetTag("agent.name", agent.Name);
        activity?.SetTag("agent.description", agent.Description ?? "");

        loggerFactory?.CreateLogger<CoordinatorAgentFactory>()
            .LogDebug("Created agent '{AgentName}'", agent.Name);

        return agent;
    }
}
