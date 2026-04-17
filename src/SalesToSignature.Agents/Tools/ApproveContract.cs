using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace SalesToSignature.Agents.Tools;

public static class ApproveContract
{
    [Description("Presents a contract summary for human review and approval. This tool pauses the pipeline until a human reviewer responds with an approval or rejection decision.")]
    public static AIFunction CreateTool()
    {
        return AIFunctionFactory.Create(RequestApproval);
    }

    [Description("Requests human approval for a contract. Returns the reviewer's decision.")]
    public static string RequestApproval(
        [Description("Executive summary of the engagement for the reviewer")] string contractSummary,
        [Description("Total contract value in USD")] decimal totalValue,
        [Description("Client name")] string clientName,
        [Description("Engagement type")] string engagementType)
    {
        // In the Foundry hosted agent workflow, this function is wrapped with
        // ToolApprovalRequestContent which pauses execution and presents the
        // tool call to the human reviewer in the VS Code / portal UI.
        // The return value here is a placeholder for local testing.
        return $"""
            APPROVAL REQUEST
            ================
            Client: {clientName}
            Engagement: {engagementType}
            Value: {totalValue:C}

            Summary:
            {contractSummary}

            [Awaiting human reviewer decision...]
            """;
    }
}
