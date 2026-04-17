using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SalesToSignature.Agents.Telemetry;

namespace SalesToSignature.Agents.Agents;

/// <summary>
/// Factory for the Contract agent. Generates contracts using LegalTemplateLookup and ClauseLibrary tools.
/// </summary>
public class ContractAgentFactory(IChatClient chatClient, IList<AITool> tools, ILoggerFactory? loggerFactory = null) : IAgentFactory
{
    public ChatClientAgent Create()
    {
        using var activity = TelemetrySetup.AgentActivitySource.StartActivity("create_agent contract");

        var agent = new ChatClientAgent(
            chatClient,
            AgentInstructions.ContractInstructions,
            name: "contract",
            description: "Generates contract documents using legal templates and clause library.",
            tools: tools);

        activity?.SetTag("gen_ai.operation.name", "create_agent");
        activity?.SetTag("gen_ai.system", "openai");
        activity?.SetTag("gen_ai.provider.name", "azure.ai.inference");
        activity?.SetTag("gen_ai.agent.name", agent.Name);
        activity?.SetTag("gen_ai.agent.description", agent.Description ?? "");
        activity?.SetTag("agent.name", agent.Name);
        activity?.SetTag("agent.tool_count", tools.Count);
        activity?.SetTag("agent.description", agent.Description ?? "");

        loggerFactory?.CreateLogger<ContractAgentFactory>()
            .LogDebug("Created agent '{AgentName}' with {ToolCount} tool(s)", agent.Name, tools.Count);

        return agent;
    }
}
