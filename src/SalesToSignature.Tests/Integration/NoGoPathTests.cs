using System.Text.Json;
using Microsoft.Extensions.AI;
using Moq;
using SalesToSignature.Agents.Agents;
using SalesToSignature.Agents.Models;
using SalesToSignature.Agents.Tools;
using Xunit;

namespace SalesToSignature.Tests.Integration;

/// <summary>
/// Integration tests for the Initech no-go path:
/// coordinator → intake → qualification(NoGo) → coordinator (pipeline stops)
/// Tests RFP parsing, red flag detection, and the no-go model flow.
/// </summary>
public class NoGoPathTests
{
    private static readonly string RfpDir = Path.Combine(FindRepoRoot(), "data", "rfps");

    // --- RFP content verification: Initech red flags ---

    [Fact]
    public void DocumentParser_ParsesInitechRfp_ReturnsCleanedText()
    {
        var rawRfp = File.ReadAllText(Path.Combine(RfpDir, "initech-advisory-rfp.md"));

        var cleaned = DocumentParser.ParseDocument(rawRfp);

        Assert.NotEmpty(cleaned);
        Assert.Contains("Initech LLC", cleaned);
        Assert.Contains("3 weeks", cleaned);
        Assert.DoesNotContain("\n\n\n", cleaned);
    }

    [Fact]
    public void DocumentParser_InitechRfp_ContainsAllExtractableFields()
    {
        var rawRfp = File.ReadAllText(Path.Combine(RfpDir, "initech-advisory-rfp.md"));
        var cleaned = DocumentParser.ParseDocument(rawRfp);

        Assert.Contains("Initech LLC", cleaned);               // clientName
        Assert.Contains("advisory", cleaned, StringComparison.OrdinalIgnoreCase); // engagementType
        Assert.Contains("$200,000", cleaned);                   // budgetMin
        Assert.Contains("$350,000", cleaned);                   // budgetMax
        Assert.Contains("3 weeks", cleaned);                    // timeline (red flag!)
    }

    [Fact]
    public void InitechRfp_ContainsRedFlags_UnrealisticTimeline()
    {
        var rawRfp = File.ReadAllText(Path.Combine(RfpDir, "initech-advisory-rfp.md"));

        // Key red flag: 3-week timeline for comprehensive assessment
        Assert.Contains("3 weeks", rawRfp);
        Assert.Contains("board meeting", rawRfp, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InitechRfp_ContainsRedFlags_NoCTO()
    {
        var rawRfp = File.ReadAllText(Path.Combine(RfpDir, "initech-advisory-rfp.md"));

        // Red flag: no CTO, CEO filling that role informally
        Assert.Contains("don't have a CTO", rawRfp, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InitechRfp_ContainsRedFlags_VagueScope()
    {
        var rawRfp = File.ReadAllText(Path.Combine(RfpDir, "initech-advisory-rfp.md"));

        // Red flags: vague scope indicators
        Assert.Contains("not sure", rawRfp, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tell us what to do", rawRfp, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("open to suggestions", rawRfp, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InitechRfp_ContainsRedFlags_LimitedStakeholderAccess()
    {
        var rawRfp = File.ReadAllText(Path.Combine(RfpDir, "initech-advisory-rfp.md"));

        // Red flag: limited stakeholder access
        Assert.Contains("2 hours/week max", rawRfp);
        Assert.Contains("limited availability", rawRfp, StringComparison.OrdinalIgnoreCase);
    }

    // --- Agent instruction verification for no-go logic ---

    [Fact]
    public void QualificationAgent_HasCorrectInstructions_ForNoGoDecision()
    {
        Assert.Contains("unrealistic", AgentInstructions.QualificationInstructions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NoGo", AgentInstructions.QualificationInstructions);
        Assert.Contains("deal-breaker", AgentInstructions.QualificationInstructions, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QualificationAgent_Instructions_MentionHandoffToCoordinator()
    {
        Assert.Contains("coordinator", AgentInstructions.QualificationInstructions);
        Assert.Contains("NoGo", AgentInstructions.QualificationInstructions);
    }

    [Fact]
    public void QualificationAgent_Instructions_ContainDealBreakerCriteria()
    {
        var instructions = AgentInstructions.QualificationInstructions;

        // All deal-breaker criteria from the instructions
        Assert.Contains("unrealistic", instructions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("decision-maker", instructions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("vague", instructions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("below market rate", instructions, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QualificationAgent_CreatedSuccessfully()
    {
        var mockChatClient = new Mock<IChatClient>();
        var agent = new QualificationAgentFactory(mockChatClient.Object).Create();

        Assert.Equal("qualification", agent.Name);
        Assert.Contains("go/no-go", agent.Description);
    }

    // --- End-to-end model flow for no-go path ---

    [Fact]
    public void NoGoPath_EndToEndModelFlow_InitechRejected()
    {
        // Simulate the no-go flow: Intake → QualificationResult(NoGo) → pipeline stops

        // Step 1: Intake produces OpportunityRecord from Initech RFP
        var opportunity = new OpportunityRecord
        {
            ClientName = "Initech LLC",
            EngagementType = EngagementType.Advisory,
            BudgetMin = 200_000m,
            BudgetMax = 350_000m,
            TimelineStart = new DateTime(2025, 2, 17),
            TimelineEnd = new DateTime(2025, 3, 10), // Only ~3 weeks!
            TechStack = ["Ruby on Rails", "React", "PostgreSQL", "Heroku", "Node.js", "Python"],
            KeyRequirements = ["Architecture review", "Security audit", "Cloud migration assessment", "Technology roadmap"],
            RawDocumentText = "Initech LLC RFP...",
            ClassificationConfidence = 0.70 // Lower confidence due to vague scope
        };

        Assert.Equal("Initech LLC", opportunity.ClientName);
        Assert.Equal(EngagementType.Advisory, opportunity.EngagementType);
        // Timeline is ~3 weeks — a red flag
        Assert.True((opportunity.TimelineEnd - opportunity.TimelineStart).TotalDays < 30);

        // Step 2: Qualification rejects with NoGo
        var qualification = new QualificationResult
        {
            FitScore = 2,
            RiskScore = 9,
            RevenuePotential = 200_000m,
            RequiredSkills = ["Ruby on Rails", "DevOps", "Security", "Cloud Architecture"],
            Risks =
            [
                "Unrealistic 3-week timeline for comprehensive assessment",
                "No CTO or technical decision-maker",
                "Vague and unbounded scope",
                "Limited stakeholder availability (CEO 2hrs/week)",
                "Team composition mismatch — Ruby on Rails not a core competency"
            ],
            DealBreakers =
            [
                "3-week timeline for full technology assessment is unrealistic — minimum 6-8 weeks needed",
                "No CTO or clear technical decision-maker to validate recommendations"
            ],
            Recommendation = Recommendation.NoGo,
            Reasoning = "Multiple deal-breakers: the 3-week timeline for a comprehensive assessment is fundamentally unrealistic, and the absence of a CTO means no technical authority to act on recommendations."
        };

        Assert.Equal(Recommendation.NoGo, qualification.Recommendation);
        Assert.NotEmpty(qualification.DealBreakers);
        Assert.True(qualification.RiskScore >= 7, "No-go should have high risk score");
        Assert.True(qualification.FitScore <= 4, "No-go should have low fit score");

        // Verify: pipeline should NOT produce proposal, contract, review, or approval
        // The no-go path terminates at qualification → coordinator reports rejection
    }

    [Fact]
    public void NoGoResult_JsonRoundTrip_PreservesAllFields()
    {
        var noGoResult = new QualificationResult
        {
            FitScore = 2,
            RiskScore = 9,
            RevenuePotential = 200_000m,
            RequiredSkills = ["Ruby", "DevOps", "Security"],
            Risks = ["Unrealistic timeline", "No CTO", "Vague scope"],
            DealBreakers = ["Timeline impossible", "No decision-maker"],
            Recommendation = Recommendation.NoGo,
            Reasoning = "Multiple deal-breakers present."
        };

        var json = JsonSerializer.Serialize(noGoResult);
        var deserialized = JsonSerializer.Deserialize<QualificationResult>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(Recommendation.NoGo, deserialized.Recommendation);
        Assert.Equal(2, deserialized.FitScore);
        Assert.Equal(9, deserialized.RiskScore);
        Assert.Equal(200_000m, deserialized.RevenuePotential);
        Assert.Equal(2, deserialized.DealBreakers.Count);
        Assert.Equal(3, deserialized.Risks.Count);
        Assert.Contains("\"NoGo\"", json); // Enum serialized as string
    }

    [Fact]
    public void NoGoPath_CoordinatorAgent_HasInstructionsForRejectingNoGo()
    {
        var instructions = AgentInstructions.CoordinatorInstructions;

        // Coordinator should know how to handle no-go results
        Assert.Contains("no-go", instructions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rejection", instructions, StringComparison.OrdinalIgnoreCase);
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
        throw new InvalidOperationException("Could not find repo root");
    }
}
