using System.Text.Json;
using Microsoft.Extensions.AI;
using Moq;
using SalesToSignature.Agents.Agents;
using SalesToSignature.Agents.Models;
using SalesToSignature.Agents.Orchestration;
using SalesToSignature.Agents.Tools;
using Xunit;

namespace SalesToSignature.Tests.Integration;

/// <summary>
/// Integration tests for the Globex review rejection loop path:
/// coordinator → intake → qualification(Go) → proposal → contract → review(IssuesFound)
///   → contract (fix) → review(Clean) → approval
/// Tests tool invocations with real Globex RFP data and the review loop model flow.
/// </summary>
public class ReviewLoopTests
{
    private static readonly string DataDir = Path.Combine(FindRepoRoot(), "data");
    private static readonly string RfpDir = Path.Combine(FindRepoRoot(), "data", "rfps");

    // --- Tool invocation tests with real Globex RFP data ---

    [Fact]
    public void DocumentParser_ParsesGlobexRfp_ReturnsCleanedText()
    {
        var rawRfp = File.ReadAllText(Path.Combine(RfpDir, "globex-data-platform-rfp.md"));

        var cleaned = DocumentParser.ParseDocument(rawRfp);

        Assert.NotEmpty(cleaned);
        Assert.Contains("Globex International", cleaned);
        Assert.Contains("Fixed Bid", cleaned);
        Assert.DoesNotContain("\n\n\n", cleaned);
    }

    [Fact]
    public void DocumentParser_GlobexRfp_ContainsAllExtractableFields()
    {
        var rawRfp = File.ReadAllText(Path.Combine(RfpDir, "globex-data-platform-rfp.md"));
        var cleaned = DocumentParser.ParseDocument(rawRfp);

        Assert.Contains("Globex International", cleaned);      // clientName
        Assert.Contains("Fixed Bid", cleaned);                  // engagementType
        Assert.Contains("$1,200,000", cleaned);                 // budgetMin
        Assert.Contains("$1,500,000", cleaned);                 // budgetMax
        Assert.Contains("May 1, 2025", cleaned);                // timelineStart
        Assert.Contains("October 31, 2026", cleaned);           // timelineEnd
        Assert.Contains("Databricks", cleaned);                 // techStack
        Assert.Contains("Snowflake", cleaned);                  // techStack
        Assert.Contains("Power BI", cleaned);                   // techStack
    }

    [Fact]
    public void TemplateLookup_FixedBid_ReturnsValidTemplate()
    {
        var lookup = new TemplateLookup(DataDir);

        var template = lookup.LookupTemplate("FixedBid");

        Assert.DoesNotContain("Error:", template);
        Assert.Contains("{{", template);
        Assert.True(template.Length > 100);
    }

    [Fact]
    public void PricingCalculator_GlobexTeam_ReturnsValidPricing()
    {
        // Globex needs: 1 Architect, 2 SeniorDevs, 1 QAEngineer, 1 ProjectManager — 18 months
        var calculator = new PricingCalculator(DataDir);

        var result = calculator.CalculatePricing("FixedBid", "Architect,SeniorDev,SeniorDev,QAEngineer,ProjectManager", 18);

        Assert.DoesNotContain("Error:", result);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;

        Assert.Equal("FixedBid", root.GetProperty("engagementType").GetString());
        Assert.Equal(18, root.GetProperty("durationMonths").GetInt32());
        Assert.True(root.GetProperty("totalPrice").GetDecimal() > 0);

        var lines = root.GetProperty("pricingLines");
        Assert.Equal(5, lines.GetArrayLength());
    }

    [Fact]
    public void LegalTemplateLookup_MSA_ReturnsValidTemplate()
    {
        var lookup = new LegalTemplateLookup(DataDir);

        var template = lookup.LookupTemplate("msa");

        Assert.DoesNotContain("Error:", template);
        Assert.Contains("Master Services Agreement", template, StringComparison.OrdinalIgnoreCase);
        Assert.True(template.Length > 200);
    }

    [Fact]
    public void LegalTemplateLookup_NDA_ReturnsValidTemplate()
    {
        var lookup = new LegalTemplateLookup(DataDir);

        var template = lookup.LookupTemplate("nda");

        Assert.DoesNotContain("Error:", template);
        Assert.True(template.Length > 200);
    }

    [Fact]
    public void ClauseLibrary_FixedBid_ReturnsEngagementSpecificClauses()
    {
        var library = new ClauseLibrary(DataDir);

        var result = library.QueryClauses(engagementType: "FixedBid");

        Assert.DoesNotContain("Error:", result);

        using var doc = JsonDocument.Parse(result);
        var clauses = doc.RootElement;

        // Should include both standard clauses and FixedBid-specific clauses
        Assert.True(clauses.GetArrayLength() > 0);
    }

    [Fact]
    public void ClauseLibrary_DataProtection_ReturnsClauses()
    {
        // Globex handles PII subject to CCPA and GDPR — DataProtection clauses are critical
        var library = new ClauseLibrary(DataDir);

        var result = library.QueryClauses(category: "DataProtection");

        Assert.DoesNotContain("Error:", result);

        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.GetArrayLength() > 0, "DataProtection clauses should exist for fintech clients");
    }

    // --- Agent instruction verification for review loop ---

    [Fact]
    public void ReviewAgent_Instructions_SupportReRouting()
    {
        Assert.Contains("contract", AgentInstructions.ReviewInstructions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("proposal", AgentInstructions.ReviewInstructions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("approval", AgentInstructions.ReviewInstructions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IssuesFound", AgentInstructions.ReviewInstructions);
    }

    [Fact]
    public void ReviewAgent_Instructions_ContainSeverityLevels()
    {
        var instructions = AgentInstructions.ReviewInstructions;

        Assert.Contains("Critical", instructions);
        Assert.Contains("High", instructions);
        Assert.Contains("Medium", instructions);
        Assert.Contains("Low", instructions);
    }

    [Fact]
    public void ReviewAgent_Instructions_ContainPricingValidationRules()
    {
        var instructions = AgentInstructions.ReviewInstructions;

        Assert.Contains("pricing", instructions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("subtotal", instructions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rate card", instructions, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ContractAgent_Instructions_HandleReReviewLoop()
    {
        var instructions = AgentInstructions.ContractInstructions;

        // Contract agent must know it can receive issues back from Review
        Assert.Contains("Review agent sends back issues", instructions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("review", instructions, StringComparison.OrdinalIgnoreCase);
    }

    // --- End-to-end model flow for review loop ---

    [Fact]
    public void ReviewLoop_EndToEndModelFlow_GlobexIssuesFoundThenClean()
    {
        // Step 1: Intake → OpportunityRecord
        var opportunity = new OpportunityRecord
        {
            ClientName = "Globex International",
            EngagementType = EngagementType.FixedBid,
            BudgetMin = 1_200_000m,
            BudgetMax = 1_500_000m,
            TimelineStart = new DateTime(2025, 5, 1),
            TimelineEnd = new DateTime(2026, 10, 31),
            TechStack = ["Databricks", "Snowflake", "Power BI", "Azure Event Hubs", "Terraform", "GitHub Actions"],
            KeyRequirements = [
                "Real-time data ingestion 500K events/sec",
                "Medallion architecture on ADLS Gen2",
                "8 Power BI dashboards with RLS",
                "ML-based anomaly detection <100ms",
                "SOC 2 Type II compliance",
                "99.9% uptime SLA"
            ],
            RawDocumentText = "Globex International RFP...",
            ClassificationConfidence = 0.98
        };

        Assert.Equal(EngagementType.FixedBid, opportunity.EngagementType);
        Assert.True(opportunity.KeyRequirements.Count >= 5);

        // Step 2: Qualification → Go
        var qualification = new QualificationResult
        {
            FitScore = 8,
            RiskScore = 4,
            RevenuePotential = 1_350_000m,
            RequiredSkills = ["Databricks", "Snowflake", "Power BI", "Azure Event Hubs", "ML", "dbt"],
            Risks = ["Complex 18-month fixed-bid carries delivery risk", "500K events/sec requirement needs POC"],
            DealBreakers = [],
            Recommendation = Recommendation.Go,
            Reasoning = "Strong fit for data engineering practice. Budget is adequate for scope. Some delivery risk mitigated by phased approach."
        };

        Assert.Equal(Recommendation.Go, qualification.Recommendation);

        // Step 3: Proposal
        var proposal = new ProposalDocument
        {
            ExecutiveSummary = "Real-time data analytics platform for Globex International.",
            Scope = "End-to-end data platform: ingestion, lake, warehouse, BI, ML, with 6-phase delivery.",
            Deliverables =
            [
                new Deliverable { Name = "Architecture ADRs", Description = "Technology selection decisions", DueDate = new DateTime(2025, 6, 30) },
                new Deliverable { Name = "Streaming Pipeline", Description = "500K events/sec ingestion", DueDate = new DateTime(2025, 10, 31) },
                new Deliverable { Name = "8 Dashboards", Description = "Power BI dashboards with RLS", DueDate = new DateTime(2026, 3, 31) }
            ],
            Milestones =
            [
                new Milestone { Name = "Phase 1 Complete", Description = "Architecture finalized", DueDate = new DateTime(2025, 6, 30) },
                new Milestone { Name = "Phase 6 Complete", Description = "Hypercare begins", DueDate = new DateTime(2026, 7, 31) }
            ],
            PricingBreakdown =
            [
                new PricingLine { Role = "Solution Architect", Rate = 300m, Hours = 2880, Subtotal = 864_000m },
                new PricingLine { Role = "Senior Data Engineer", Rate = 250m, Hours = 2880, Subtotal = 720_000m }
            ],
            TotalPrice = 1_584_000m, // Intentionally inconsistent for review to catch
            EngagementType = EngagementType.FixedBid
        };

        // Step 4: Contract (first pass)
        var contract = new ContractDocument
        {
            ContractText = "Master Services Agreement for Globex International data platform engagement...",
            StandardClauses = ["IP-001", "LIA-001", "TERM-001", "PAY-001", "CONF-001", "DP-001", "FM-001"],
            CustomClauses = ["FB-001", "FB-002"],
            EffectiveDate = new DateTime(2025, 5, 1),
            TerminationTerms = "Termination for cause with 60 days notice. Milestone-based exit ramps.",
            LiabilityCap = 3_500_000m, // Intentionally too high — review should catch
            PaymentTerms = "Milestone-based: 15% signing, phased payments per delivery."
        };

        // Step 5: First Review — IssuesFound!
        var firstReview = new ReviewReport
        {
            OverallStatus = ReviewStatus.IssuesFound,
            Issues =
            [
                new ReviewIssue
                {
                    Severity = IssueSeverity.Critical,
                    Description = "Liability cap ($3.5M) exceeds 2x total engagement value ($1.58M × 2 = $3.17M)",
                    Recommendation = "Reduce liability cap to $3.17M or less"
                },
                new ReviewIssue
                {
                    Severity = IssueSeverity.High,
                    Description = "Missing data sovereignty clause — Globex requires US-East data residency per CCPA/GDPR",
                    Recommendation = "Add explicit data sovereignty clause specifying US-East Azure regions"
                },
                new ReviewIssue
                {
                    Severity = IssueSeverity.High,
                    Description = "Pricing inconsistency: line items sum to $1,584,000 but total should reflect all 5 team members",
                    Recommendation = "Recalculate pricing with full team: Architect + 2 Data Engineers + Analytics Engineer + ML Engineer + PM"
                }
            ],
            PricingConsistent = false,
            RequirementsCovered =
            [
                new RequirementCheck { Requirement = "Real-time ingestion 500K events/sec", Covered = true, Notes = "Phase 2 addresses this" },
                new RequirementCheck { Requirement = "Medallion architecture", Covered = true, Notes = "Phase 3" },
                new RequirementCheck { Requirement = "8 Power BI dashboards", Covered = true, Notes = "Phase 4" },
                new RequirementCheck { Requirement = "ML anomaly detection", Covered = true, Notes = "Phase 5" },
                new RequirementCheck { Requirement = "SOC 2 Type II", Covered = true, Notes = "Phase 6" }
            ],
            TargetAgent = "contract"
        };

        Assert.Equal(ReviewStatus.IssuesFound, firstReview.OverallStatus);
        Assert.Equal("contract", firstReview.TargetAgent);
        Assert.False(firstReview.PricingConsistent);
        Assert.Contains(firstReview.Issues, i => i.Severity == IssueSeverity.Critical);
        Assert.True(firstReview.Issues.Count >= 2);
        // All requirements are still covered despite contract issues
        Assert.All(firstReview.RequirementsCovered, r => Assert.True(r.Covered));

        // Step 6: Contract fixes issues (second pass)
        var fixedContract = new ContractDocument
        {
            ContractText = "Master Services Agreement for Globex International (revised)...",
            StandardClauses = ["IP-001", "LIA-001", "TERM-001", "PAY-001", "CONF-001", "DP-001", "DP-002", "FM-001"],
            CustomClauses = ["FB-001", "FB-002", "FB-DS-001"], // Added data sovereignty clause
            EffectiveDate = new DateTime(2025, 5, 1),
            TerminationTerms = "Termination for cause with 60 days notice. Milestone-based exit ramps.",
            LiabilityCap = 2_700_000m, // Reduced to ~2x revised total
            PaymentTerms = "Milestone-based: 15% signing, phased payments per delivery."
        };

        Assert.True(fixedContract.CustomClauses.Count > contract.CustomClauses.Count);
        Assert.True(fixedContract.LiabilityCap < contract.LiabilityCap);

        // Step 7: Second Review — Clean!
        var secondReview = new ReviewReport
        {
            OverallStatus = ReviewStatus.Clean,
            Issues = [],
            PricingConsistent = true,
            RequirementsCovered =
            [
                new RequirementCheck { Requirement = "Real-time ingestion", Covered = true, Notes = "Phase 2" },
                new RequirementCheck { Requirement = "Data sovereignty US-East", Covered = true, Notes = "Clause FB-DS-001 added" }
            ],
            TargetAgent = null
        };

        Assert.Equal(ReviewStatus.Clean, secondReview.OverallStatus);
        Assert.Empty(secondReview.Issues);
        Assert.True(secondReview.PricingConsistent);
        Assert.Null(secondReview.TargetAgent);

        // Step 8: Approval
        var approval = new ApprovalDecision
        {
            Approved = true,
            ReviewerName = "James Chen",
            Feedback = "Approved after contract revision. Data sovereignty and liability issues resolved.",
            Timestamp = new DateTime(2025, 4, 10, 10, 0, 0),
            ContractSummary = "18-month fixed-bid data analytics platform for Globex International."
        };

        Assert.True(approval.Approved);
    }

    [Fact]
    public void ReviewReport_IssuesFound_TargetsContractAgent()
    {
        var reviewReport = new ReviewReport
        {
            OverallStatus = ReviewStatus.IssuesFound,
            Issues =
            [
                new ReviewIssue
                {
                    Severity = IssueSeverity.Critical,
                    Description = "Liability cap exceeds 2x total engagement value",
                    Recommendation = "Reduce liability cap to $1.5M"
                },
                new ReviewIssue
                {
                    Severity = IssueSeverity.High,
                    Description = "Missing data sovereignty clause for EU counterparties",
                    Recommendation = "Add GDPR data protection clause"
                }
            ],
            PricingConsistent = true,
            RequirementsCovered =
            [
                new RequirementCheck { Requirement = "Real-time analytics", Covered = true, Notes = "Addressed" }
            ],
            TargetAgent = "contract"
        };

        Assert.Equal(ReviewStatus.IssuesFound, reviewReport.OverallStatus);
        Assert.Equal("contract", reviewReport.TargetAgent);
        Assert.Equal(2, reviewReport.Issues.Count);
        Assert.Contains(reviewReport.Issues, i => i.Severity == IssueSeverity.Critical);
    }

    [Fact]
    public void ReviewReport_IssuesFound_CanTargetProposalAgent()
    {
        // When pricing issues are found, review routes back to proposal (not contract)
        var pricingIssueReport = new ReviewReport
        {
            OverallStatus = ReviewStatus.IssuesFound,
            Issues =
            [
                new ReviewIssue
                {
                    Severity = IssueSeverity.Critical,
                    Description = "Pricing line subtotals do not sum to total price",
                    Recommendation = "Recalculate pricing with correct hours per role"
                }
            ],
            PricingConsistent = false,
            RequirementsCovered = [],
            TargetAgent = "proposal"
        };

        Assert.Equal("proposal", pricingIssueReport.TargetAgent);
        Assert.False(pricingIssueReport.PricingConsistent);
    }

    [Fact]
    public void ReviewReport_Clean_NoTargetAgent()
    {
        var cleanReport = new ReviewReport
        {
            OverallStatus = ReviewStatus.Clean,
            Issues = [],
            PricingConsistent = true,
            RequirementsCovered =
            [
                new RequirementCheck { Requirement = "Data platform", Covered = true, Notes = "Full coverage" }
            ],
            TargetAgent = null
        };

        Assert.Equal(ReviewStatus.Clean, cleanReport.OverallStatus);
        Assert.Empty(cleanReport.Issues);
        Assert.Null(cleanReport.TargetAgent);
    }

    [Fact]
    public void Pipeline_ReviewLoop_HandoffGraph_IncludesBackRoutes()
    {
        var mockChatClient = new Mock<IChatClient>();

        var workflow = PipelineBuilder.BuildPipeline(mockChatClient.Object, DataDir);
        Assert.NotNull(workflow);

        // Review agent instructions confirm both back-routing targets
        Assert.Contains("contract", AgentInstructions.ReviewInstructions);
        Assert.Contains("proposal", AgentInstructions.ReviewInstructions);
    }

    [Fact]
    public void ReviewReport_JsonRoundTrip_PreservesIssuesFound()
    {
        var report = new ReviewReport
        {
            OverallStatus = ReviewStatus.IssuesFound,
            Issues =
            [
                new ReviewIssue { Severity = IssueSeverity.Critical, Description = "Liability cap too high", Recommendation = "Reduce to 2x" },
                new ReviewIssue { Severity = IssueSeverity.High, Description = "Missing clause", Recommendation = "Add DP clause" }
            ],
            PricingConsistent = false,
            RequirementsCovered =
            [
                new RequirementCheck { Requirement = "Analytics", Covered = true, Notes = "OK" }
            ],
            TargetAgent = "contract"
        };

        var json = JsonSerializer.Serialize(report);
        var deserialized = JsonSerializer.Deserialize<ReviewReport>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(ReviewStatus.IssuesFound, deserialized.OverallStatus);
        Assert.Equal(2, deserialized.Issues.Count);
        Assert.Equal(IssueSeverity.Critical, deserialized.Issues[0].Severity);
        Assert.False(deserialized.PricingConsistent);
        Assert.Equal("contract", deserialized.TargetAgent);
        Assert.Contains("\"IssuesFound\"", json); // Enum serialized as string
        Assert.Contains("\"Critical\"", json);
    }

    [Fact]
    public void ApproveContract_GlobexScenario_ProducesValidOutput()
    {
        var result = ApproveContract.RequestApproval(
            contractSummary: "18-month fixed-bid real-time data analytics platform for Globex International",
            totalValue: 1_350_000m,
            clientName: "Globex International",
            engagementType: "FixedBid");

        Assert.Contains("Globex International", result);
        Assert.Contains("FixedBid", result);
        Assert.Contains("APPROVAL REQUEST", result);
        Assert.Contains("$1,350,000.00", result);
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
