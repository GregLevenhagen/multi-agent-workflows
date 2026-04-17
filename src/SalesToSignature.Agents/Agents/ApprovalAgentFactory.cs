using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SalesToSignature.Agents.Telemetry;

namespace SalesToSignature.Agents.Agents;

/// <summary>
/// Factory for the Approval agent. Presents final engagement package for human review using ApproveContract tool.
/// This is the terminal agent in the pipeline — human-in-the-loop approval pauses execution.
/// </summary>
public class ApprovalAgentFactory(IChatClient chatClient, IList<AITool> tools, ILoggerFactory? loggerFactory = null) : IAgentFactory
{
    public ChatClientAgent Create()
    {
        using var activity = TelemetrySetup.AgentActivitySource.StartActivity("create_agent approval");

        var agent = new ChatClientAgent(
            chatClient,
            AgentInstructions.ApprovalInstructions,
            name: "approval",
            description: "Presents final engagement package for human review and captures approval decision.",
            tools: tools);

        activity?.SetTag("gen_ai.operation.name", "create_agent");
        activity?.SetTag("gen_ai.system", "openai");
        activity?.SetTag("gen_ai.provider.name", "azure.ai.inference");
        activity?.SetTag("gen_ai.agent.name", agent.Name);
        activity?.SetTag("gen_ai.agent.description", agent.Description ?? "");
        activity?.SetTag("agent.name", agent.Name);
        activity?.SetTag("agent.tool_count", tools.Count);
        activity?.SetTag("agent.description", agent.Description ?? "");

        loggerFactory?.CreateLogger<ApprovalAgentFactory>()
            .LogDebug("Created agent '{AgentName}' with {ToolCount} tool(s)", agent.Name, tools.Count);

        return agent;
    }
}
