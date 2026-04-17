using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SalesToSignature.Agents.Telemetry;

namespace SalesToSignature.Agents.Agents;

/// <summary>
/// Factory for the Qualification agent. Evaluates opportunity fit/risk and makes Go/NoGo recommendation.
/// </summary>
public class QualificationAgentFactory(IChatClient chatClient, ILoggerFactory? loggerFactory = null) : IAgentFactory
{
    public ChatClientAgent Create()
    {
        using var activity = TelemetrySetup.AgentActivitySource.StartActivity("create_agent qualification");

        var agent = new ChatClientAgent(
            chatClient,
            AgentInstructions.QualificationInstructions,
            name: "qualification",
            description: "Evaluates opportunity fit, risk, and makes go/no-go recommendation.");

        activity?.SetTag("gen_ai.operation.name", "create_agent");
        activity?.SetTag("gen_ai.system", "openai");
        activity?.SetTag("gen_ai.provider.name", "azure.ai.inference");
        activity?.SetTag("gen_ai.agent.name", agent.Name);
        activity?.SetTag("gen_ai.agent.description", agent.Description ?? "");
        activity?.SetTag("agent.name", agent.Name);
        activity?.SetTag("agent.description", agent.Description ?? "");

        loggerFactory?.CreateLogger<QualificationAgentFactory>()
            .LogDebug("Created agent '{AgentName}'", agent.Name);

        return agent;
    }
}
