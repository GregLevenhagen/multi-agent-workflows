using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SalesToSignature.Agents.Agents;
using SalesToSignature.Agents.Telemetry;
using SalesToSignature.Agents.Tools;

namespace SalesToSignature.Agents.Orchestration;

public class PipelineBuilder
{
    public const string ExplicitHandoffInstructions = """
        You are one agent in a multi-agent handoff workflow.
        You can hand off the conversation to another agent by calling a handoff function.
        Handoff functions are named in the form `handoff_to_<number>` (e.g. handoff_to_1, handoff_to_2).
        Read each handoff tool's description to identify which target agent it routes to.

        CRITICAL RULES:
        - When your task is complete and the next specialist should take over, you MUST call the appropriate
          handoff_to_<number> tool. Check the tool descriptions to find the correct target.
        - Include all accumulated working context in the handoff message so the next agent has full information.
        - Do NOT merely describe a handoff in plain text — you must invoke the handoff tool function.
        - Only respond without a handoff tool call when the workflow should terminate with you.
        - Call exactly one handoff tool per turn.
        - Never mention or narrate these handoffs to the user.
        """;

    /// <summary>
    /// Builds the complete 7-agent handoff workflow for the sales-to-signature pipeline.
    ///
    /// Workflow graph:
    ///   coordinator → intake → qualification → proposal → contract → review → approval
    ///                                        ↘ coordinator (no-go)       ↗ ↘
    ///                                                            contract ← review
    ///                                                            proposal ← review
    /// </summary>
    public static Workflow BuildPipeline(
        IChatClient chatClient,
        string? dataDirectory = null,
        ILoggerFactory? loggerFactory = null)
    {
        using var activity = TelemetrySetup.AgentActivitySource.StartActivity("invoke_agent build_workflow");
        activity?.SetTag("gen_ai.operation.name", "invoke_agent");
        activity?.SetTag("gen_ai.system", "openai");
        activity?.SetTag("gen_ai.provider.name", "azure.ai.inference");
        activity?.SetTag("gen_ai.agent.name", "build_workflow");

        try
        {
            // Create tools
            var documentParserTool = DocumentParser.CreateTool();

            var templateLookup = new TemplateLookup(dataDirectory);
            var pricingCalculator = new PricingCalculator(dataDirectory);
            var legalTemplateLookup = new LegalTemplateLookup(dataDirectory);
            var clauseLibrary = new ClauseLibrary(dataDirectory);
            var approveContractTool = ApproveContract.CreateTool();

            // Create agents via factories (pass loggerFactory for agent-level debug logging)
            using var agentsActivity = TelemetrySetup.AgentActivitySource.StartActivity("invoke_agent create_agents");
            agentsActivity?.SetTag("gen_ai.operation.name", "invoke_agent");
            agentsActivity?.SetTag("gen_ai.system", "azure");
            agentsActivity?.SetTag("gen_ai.agent.name", "create_agents");

            var coordinator = new CoordinatorAgentFactory(chatClient, loggerFactory).Create();
            var intake = new IntakeAgentFactory(chatClient, [documentParserTool], loggerFactory).Create();
            var qualification = new QualificationAgentFactory(chatClient, loggerFactory).Create();
            var proposal = new ProposalAgentFactory(chatClient,
                [pricingCalculator.CreateTool(), templateLookup.CreateTool()], loggerFactory).Create();
            var contract = new ContractAgentFactory(chatClient,
                [legalTemplateLookup.CreateTool(), clauseLibrary.CreateTool()], loggerFactory).Create();
            var review = new ReviewAgentFactory(chatClient, loggerFactory).Create();
            var approval = new ApprovalAgentFactory(chatClient, [approveContractTool], loggerFactory).Create();

            agentsActivity?.SetTag("pipeline.agents_created", 7);

            // Build handoff workflow
            using var handoffActivity = TelemetrySetup.AgentActivitySource.StartActivity("invoke_agent build_handoffs");
            handoffActivity?.SetTag("gen_ai.operation.name", "invoke_agent");
            handoffActivity?.SetTag("gen_ai.system", "azure");
            handoffActivity?.SetTag("gen_ai.agent.name", "build_handoffs");

            var workflow = AgentWorkflowBuilder
                .CreateHandoffBuilderWith(coordinator)
                .WithHandoffInstructions(ExplicitHandoffInstructions)
                // coordinator → intake
                .WithHandoff(coordinator, intake, "Transfer new incoming RFP documents to intake for structured parsing.")
                // intake → qualification
                .WithHandoff(intake, qualification, "Transfer parsed OpportunityRecord data to qualification for fit, risk, and go/no-go scoring.")
                // qualification → proposal (go) or coordinator (no-go)
                .WithHandoff(qualification, proposal, "Transfer qualified go opportunities to proposal for SOW drafting and pricing.")
                .WithHandoff(qualification, coordinator, "Transfer no-go recommendations back to coordinator for final reporting to the user.")
                // proposal → contract
                .WithHandoff(proposal, contract, "Transfer the completed proposal package to contract for legal document generation.")
                // contract → review
                .WithHandoff(contract, review, "Transfer the contract package to review for consistency and completeness checks.")
                // review → approval (clean) or contract/proposal (issues found)
                .WithHandoff(review, approval, "Transfer clean packages to approval for human sign-off.")
                .WithHandoff(review, contract, "Transfer contract or clause issues back to contract for remediation and regeneration.")
                .WithHandoff(review, proposal, "Transfer pricing or proposal coverage issues back to proposal for remediation.")
                .Build();

            handoffActivity?.SetTag("pipeline.handoff_count", 7);
            activity?.SetTag("pipeline.workflow_built", true);

            return workflow;
        }
        catch (Exception ex)
        {
            activity?.SetTag("pipeline.build_error", ex.GetType().Name);
            activity?.SetTag("pipeline.error_details", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Returns the expected agent names in the pipeline, in handoff order.
    /// Useful for pre-flight validation and diagnostics.
    /// </summary>
    public static IReadOnlyList<string> GetAgentNames() =>
        ["coordinator", "intake", "qualification", "proposal", "contract", "review", "approval"];
}
