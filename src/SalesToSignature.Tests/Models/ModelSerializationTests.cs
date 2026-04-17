using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using SalesToSignature.Agents.Models;
using Xunit;

namespace SalesToSignature.Tests.Models;

public class ModelSerializationTests
{

    [Fact]
    public void OpportunityRecord_RoundTrip()
    {
        var original = new OpportunityRecord
        {
            ClientName = "Acme Corp",
            EngagementType = EngagementType.StaffAugmentation,
            BudgetMin = 500_000m,
            BudgetMax = 750_000m,
            TimelineStart = new DateTime(2025, 4, 1),
            TimelineEnd = new DateTime(2026, 3, 31),
            TechStack = [".NET 9", "Azure", "Kubernetes"],
            KeyRequirements = ["CI/CD", "Cloud migration"],
            RawDocumentText = "Sample RFP text",
            ClassificationConfidence = 0.95
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<OpportunityRecord>(json, PipelineJsonOptions.Default)!;

        Assert.Equal(original.ClientName, deserialized.ClientName);
        Assert.Equal(original.EngagementType, deserialized.EngagementType);
        Assert.Equal(original.BudgetMin, deserialized.BudgetMin);
        Assert.Equal(original.BudgetMax, deserialized.BudgetMax);
        Assert.Equal(original.TechStack.Count, deserialized.TechStack.Count);
        Assert.Equal(original.ClassificationConfidence, deserialized.ClassificationConfidence);
    }

    [Fact]
    public void QualificationResult_RoundTrip()
    {
        var original = new QualificationResult
        {
            FitScore = 8,
            RiskScore = 3,
            RevenuePotential = 625_000m,
            RequiredSkills = [".NET", "Azure", "DevOps"],
            Risks = ["Tight timeline"],
            DealBreakers = [],
            Recommendation = Recommendation.Go,
            Reasoning = "Strong fit for our capabilities"
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<QualificationResult>(json, PipelineJsonOptions.Default)!;

        Assert.Equal(original.FitScore, deserialized.FitScore);
        Assert.Equal(original.Recommendation, deserialized.Recommendation);
        Assert.Empty(deserialized.DealBreakers);
        Assert.Contains("Go", json); // Enum serializes as string
    }

    [Fact]
    public void ProposalDocument_RoundTrip()
    {
        var original = new ProposalDocument
        {
            ExecutiveSummary = "Cloud migration proposal",
            Scope = "Migrate 14 apps to Azure",
            Deliverables =
            [
                new Deliverable { Name = "Assessment Report", Description = "Full assessment", DueDate = new DateTime(2025, 6, 1) }
            ],
            Milestones =
            [
                new Milestone { Name = "Phase 1 Complete", Description = "Planning done", DueDate = new DateTime(2025, 6, 1) }
            ],
            PricingBreakdown =
            [
                new PricingLine { Role = "Architect", Rate = 275m, Hours = 1920, Subtotal = 528_000m }
            ],
            TotalPrice = 528_000m,
            EngagementType = EngagementType.StaffAugmentation
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<ProposalDocument>(json, PipelineJsonOptions.Default)!;

        Assert.Equal(original.TotalPrice, deserialized.TotalPrice);
        Assert.Single(deserialized.Deliverables);
        Assert.Single(deserialized.PricingBreakdown);
        Assert.Equal("Architect", deserialized.PricingBreakdown[0].Role);
    }

    [Fact]
    public void ContractDocument_RoundTrip()
    {
        var original = new ContractDocument
        {
            ContractText = "Master Services Agreement...",
            StandardClauses = ["IP-001", "LIA-001", "TERM-001"],
            CustomClauses = ["SA-001", "SA-002"],
            EffectiveDate = new DateTime(2025, 4, 1),
            TerminationTerms = "30 days written notice",
            LiabilityCap = 750_000m,
            PaymentTerms = "Net 30"
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<ContractDocument>(json, PipelineJsonOptions.Default)!;

        Assert.Equal(original.LiabilityCap, deserialized.LiabilityCap);
        Assert.Equal(3, deserialized.StandardClauses.Count);
        Assert.Equal(2, deserialized.CustomClauses.Count);
    }

    [Fact]
    public void ReviewReport_RoundTrip()
    {
        var original = new ReviewReport
        {
            OverallStatus = ReviewStatus.IssuesFound,
            Issues =
            [
                new ReviewIssue { Severity = IssueSeverity.High, Description = "Pricing error", Recommendation = "Recalculate" }
            ],
            PricingConsistent = false,
            RequirementsCovered =
            [
                new RequirementCheck { Requirement = "CI/CD", Covered = true, Notes = "Addressed in scope" }
            ],
            TargetAgent = "proposal"
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<ReviewReport>(json, PipelineJsonOptions.Default)!;

        Assert.Equal(ReviewStatus.IssuesFound, deserialized.OverallStatus);
        Assert.Single(deserialized.Issues);
        Assert.Equal(IssueSeverity.High, deserialized.Issues[0].Severity);
        Assert.Equal("proposal", deserialized.TargetAgent);
        Assert.Contains("IssuesFound", json); // Enum as string
    }

    [Fact]
    public void ApprovalDecision_RoundTrip()
    {
        var original = new ApprovalDecision
        {
            Approved = true,
            ReviewerName = "Jane Smith",
            Feedback = "Looks good, proceed",
            Timestamp = new DateTime(2025, 3, 15, 14, 30, 0),
            ContractSummary = "Acme Corp staff augmentation, $625K"
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<ApprovalDecision>(json, PipelineJsonOptions.Default)!;

        Assert.True(deserialized.Approved);
        Assert.Equal("Jane Smith", deserialized.ReviewerName);
        Assert.Equal(original.Timestamp, deserialized.Timestamp);
    }

    [Theory]
    [InlineData(EngagementType.StaffAugmentation, "StaffAugmentation")]
    [InlineData(EngagementType.FixedBid, "FixedBid")]
    [InlineData(EngagementType.TimeAndMaterials, "TimeAndMaterials")]
    [InlineData(EngagementType.Advisory, "Advisory")]
    public void EngagementType_SerializesAsString(EngagementType value, string expectedString)
    {
        var json = JsonSerializer.Serialize(value);

        Assert.Contains(expectedString, json);
    }

    [Theory]
    [InlineData(Recommendation.Go, "Go")]
    [InlineData(Recommendation.NoGo, "NoGo")]
    public void Recommendation_SerializesAsString(Recommendation value, string expectedString)
    {
        var json = JsonSerializer.Serialize(value);

        Assert.Contains(expectedString, json);
    }

    [Fact]
    public void OpportunityRecord_DefaultCollections_NotNull()
    {
        var record = new OpportunityRecord();

        Assert.NotNull(record.TechStack);
        Assert.Empty(record.TechStack);
        Assert.NotNull(record.KeyRequirements);
        Assert.Empty(record.KeyRequirements);
    }

    [Fact]
    public void ReviewReport_DefaultCollections_NotNull()
    {
        var report = new ReviewReport();

        Assert.NotNull(report.Issues);
        Assert.Empty(report.Issues);
        Assert.NotNull(report.RequirementsCovered);
        Assert.Empty(report.RequirementsCovered);
        Assert.Null(report.TargetAgent);
    }

    [Fact]
    public void ContractDocument_DefaultCollections_NotNull()
    {
        var contract = new ContractDocument();

        Assert.NotNull(contract.StandardClauses);
        Assert.Empty(contract.StandardClauses);
        Assert.NotNull(contract.CustomClauses);
        Assert.Empty(contract.CustomClauses);
    }

    [Fact]
    public void ProposalDocument_DefaultCollections_NotNull()
    {
        var proposal = new ProposalDocument();

        Assert.NotNull(proposal.Deliverables);
        Assert.Empty(proposal.Deliverables);
        Assert.NotNull(proposal.Milestones);
        Assert.Empty(proposal.Milestones);
        Assert.NotNull(proposal.PricingBreakdown);
        Assert.Empty(proposal.PricingBreakdown);
    }

    // --- Parameterized enum tests for ReviewStatus and IssueSeverity ---

    [Theory]
    [InlineData(ReviewStatus.Clean, "Clean")]
    [InlineData(ReviewStatus.IssuesFound, "IssuesFound")]
    public void ReviewStatus_SerializesAsString(ReviewStatus value, string expectedString)
    {
        var json = JsonSerializer.Serialize(value);

        Assert.Contains(expectedString, json);
    }

    [Theory]
    [InlineData(IssueSeverity.Low, "Low")]
    [InlineData(IssueSeverity.Medium, "Medium")]
    [InlineData(IssueSeverity.High, "High")]
    [InlineData(IssueSeverity.Critical, "Critical")]
    public void IssueSeverity_SerializesAsString(IssueSeverity value, string expectedString)
    {
        var json = JsonSerializer.Serialize(value);

        Assert.Contains(expectedString, json);
    }

    [Theory]
    [InlineData("Clean", ReviewStatus.Clean)]
    [InlineData("IssuesFound", ReviewStatus.IssuesFound)]
    public void ReviewStatus_DeserializesFromString(string jsonValue, ReviewStatus expected)
    {
        var deserialized = JsonSerializer.Deserialize<ReviewStatus>($"\"{jsonValue}\"");

        Assert.Equal(expected, deserialized);
    }

    [Theory]
    [InlineData("Low", IssueSeverity.Low)]
    [InlineData("Medium", IssueSeverity.Medium)]
    [InlineData("High", IssueSeverity.High)]
    [InlineData("Critical", IssueSeverity.Critical)]
    public void IssueSeverity_DeserializesFromString(string jsonValue, IssueSeverity expected)
    {
        var deserialized = JsonSerializer.Deserialize<IssueSeverity>($"\"{jsonValue}\"");

        Assert.Equal(expected, deserialized);
    }

    // --- Record value-based equality tests ---

    // Records WITHOUT collections use value-based equality on all properties.
    // Records WITH List<T> properties use reference equality for those lists,
    // so two records with identical list contents but different list instances are NOT equal.

    [Fact]
    public void ReviewIssue_ValueEquality_NoCollections()
    {
        var a = new ReviewIssue { Severity = IssueSeverity.High, Description = "Pricing error", Recommendation = "Fix" };
        var b = new ReviewIssue { Severity = IssueSeverity.High, Description = "Pricing error", Recommendation = "Fix" };

        Assert.Equal(a, b);
        Assert.NotSame(a, b);
    }

    [Fact]
    public void RequirementCheck_ValueEquality_NoCollections()
    {
        var a = new RequirementCheck { Requirement = "CI/CD", Covered = true, Notes = "Done" };
        var b = new RequirementCheck { Requirement = "CI/CD", Covered = true, Notes = "Done" };

        Assert.Equal(a, b);
    }

    [Fact]
    public void ApprovalDecision_ValueEquality_NoCollections()
    {
        var ts = new DateTime(2025, 4, 1, 12, 0, 0);
        var a = new ApprovalDecision { Approved = true, ReviewerName = "Jane", Timestamp = ts };
        var b = new ApprovalDecision { Approved = true, ReviewerName = "Jane", Timestamp = ts };

        Assert.Equal(a, b);
    }

    [Fact]
    public void Record_Inequality_DifferentValues()
    {
        var a = new ReviewIssue { Severity = IssueSeverity.High, Description = "Error A" };
        var b = new ReviewIssue { Severity = IssueSeverity.Low, Description = "Error B" };

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Record_WithCollections_NotEqual_DifferentListInstances()
    {
        // List<T> uses reference equality, so two records with identical list contents
        // but separate list instances are NOT equal — this is a known C# record behavior.
        var a = new OpportunityRecord { ClientName = "Acme" };
        var b = new OpportunityRecord { ClientName = "Acme" };

        Assert.NotEqual(a, b); // Different List<string> instances for TechStack/KeyRequirements
    }

    [Fact]
    public void Record_WithSharedCollections_AreEqual()
    {
        var skills = new List<string> { ".NET", "Azure" };
        var a = new QualificationResult { FitScore = 8, RequiredSkills = skills, Risks = skills, DealBreakers = skills };
        var b = new QualificationResult { FitScore = 8, RequiredSkills = skills, Risks = skills, DealBreakers = skills };

        Assert.Equal(a, b); // Same list references → records are equal
    }

    // --- Negative deserialization tests ---

    [Fact]
    public void OpportunityRecord_MissingFields_GetsDefaults()
    {
        var json = "{}";
        var deserialized = JsonSerializer.Deserialize<OpportunityRecord>(json, PipelineJsonOptions.Default)!;

        Assert.Equal(string.Empty, deserialized.ClientName);
        Assert.Equal(0m, deserialized.BudgetMin);
        Assert.Empty(deserialized.TechStack);
        Assert.Equal(0.0, deserialized.ClassificationConfidence);
    }

    [Fact]
    public void QualificationResult_MissingFields_GetsDefaults()
    {
        var json = "{}";
        var deserialized = JsonSerializer.Deserialize<QualificationResult>(json, PipelineJsonOptions.Default)!;

        Assert.Equal(0, deserialized.FitScore);
        Assert.Equal(0m, deserialized.RevenuePotential);
        Assert.Empty(deserialized.RequiredSkills);
        Assert.Empty(deserialized.DealBreakers);
        Assert.Equal(string.Empty, deserialized.Reasoning);
    }

    [Fact]
    public void ReviewReport_MissingFields_GetsDefaults()
    {
        var json = "{}";
        var deserialized = JsonSerializer.Deserialize<ReviewReport>(json, PipelineJsonOptions.Default)!;

        Assert.Equal(default, deserialized.OverallStatus);
        Assert.Empty(deserialized.Issues);
        Assert.False(deserialized.PricingConsistent);
        Assert.Null(deserialized.TargetAgent);
    }

    [Fact]
    public void ContractDocument_MissingFields_GetsDefaults()
    {
        var json = "{}";
        var deserialized = JsonSerializer.Deserialize<ContractDocument>(json, PipelineJsonOptions.Default)!;

        Assert.Equal(string.Empty, deserialized.ContractText);
        Assert.Empty(deserialized.StandardClauses);
        Assert.Equal(0m, deserialized.LiabilityCap);
    }

    [Fact]
    public void MalformedJson_ThrowsJsonException()
    {
        var malformed = "{ invalid json }";

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<OpportunityRecord>(malformed, PipelineJsonOptions.Default));
    }

    [Fact]
    public void ExtraFields_IgnoredDuringDeserialization()
    {
        var json = """{"clientName": "Acme", "unknownField": 42, "anotherUnknown": "value"}""";
        var deserialized = JsonSerializer.Deserialize<OpportunityRecord>(json, PipelineJsonOptions.Default)!;

        Assert.Equal("Acme", deserialized.ClientName);
    }

    [Fact]
    public void CaseInsensitive_Deserialization()
    {
        var json = """{"CLIENTNAME": "Acme", "BUDGETMIN": 100000}""";
        var deserialized = JsonSerializer.Deserialize<OpportunityRecord>(json, PipelineJsonOptions.Default)!;

        Assert.Equal("Acme", deserialized.ClientName);
        Assert.Equal(100_000m, deserialized.BudgetMin);
    }

    // --- IValidatableObject tests ---

    private static bool TryValidate(object model, out List<ValidationResult> results)
    {
        results = [];
        var context = new ValidationContext(model);
        return Validator.TryValidateObject(model, context, results, validateAllProperties: true);
    }

    [Fact]
    public void OpportunityRecord_Valid_PassesValidation()
    {
        var record = new OpportunityRecord
        {
            ClientName = "Acme Corp",
            BudgetMin = 500_000m,
            BudgetMax = 750_000m,
            TimelineStart = new DateTime(2025, 4, 1),
            TimelineEnd = new DateTime(2026, 3, 31),
            ClassificationConfidence = 0.95
        };

        Assert.True(TryValidate(record, out var results));
        Assert.Empty(results);
    }

    [Fact]
    public void OpportunityRecord_BudgetMinGreaterThanMax_FailsValidation()
    {
        var record = new OpportunityRecord { BudgetMin = 800_000m, BudgetMax = 500_000m };

        Assert.False(TryValidate(record, out var results));
        Assert.Contains(results, r => r.MemberNames.Contains("BudgetMin"));
    }

    [Fact]
    public void OpportunityRecord_TimelineStartAfterEnd_FailsValidation()
    {
        var record = new OpportunityRecord
        {
            TimelineStart = new DateTime(2026, 6, 1),
            TimelineEnd = new DateTime(2025, 1, 1)
        };

        Assert.False(TryValidate(record, out var results));
        Assert.Contains(results, r => r.MemberNames.Contains("TimelineStart"));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(5.0)]
    public void OpportunityRecord_ClassificationConfidenceOutOfRange_FailsValidation(double confidence)
    {
        var record = new OpportunityRecord { ClassificationConfidence = confidence };

        Assert.False(TryValidate(record, out var results));
        Assert.Contains(results, r => r.MemberNames.Contains("ClassificationConfidence"));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void OpportunityRecord_ClassificationConfidenceInRange_PassesValidation(double confidence)
    {
        var record = new OpportunityRecord { ClassificationConfidence = confidence };

        Assert.True(TryValidate(record, out _));
    }

    [Fact]
    public void QualificationResult_Valid_PassesValidation()
    {
        var result = new QualificationResult
        {
            FitScore = 8,
            RiskScore = 3,
            Recommendation = Recommendation.Go,
            Reasoning = "Strong fit"
        };

        Assert.True(TryValidate(result, out var results));
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public void QualificationResult_FitScoreOutOfRange_FailsValidation(int fitScore)
    {
        var result = new QualificationResult { FitScore = fitScore, RiskScore = 5 };

        Assert.False(TryValidate(result, out var results));
        Assert.Contains(results, r => r.MemberNames.Contains("FitScore"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-5)]
    public void QualificationResult_RiskScoreOutOfRange_FailsValidation(int riskScore)
    {
        var result = new QualificationResult { FitScore = 5, RiskScore = riskScore };

        Assert.False(TryValidate(result, out var results));
        Assert.Contains(results, r => r.MemberNames.Contains("RiskScore"));
    }

    [Fact]
    public void QualificationResult_NoGoWithoutReasoning_FailsValidation()
    {
        var result = new QualificationResult
        {
            FitScore = 2,
            RiskScore = 9,
            Recommendation = Recommendation.NoGo,
            Reasoning = ""
        };

        Assert.False(TryValidate(result, out var results));
        Assert.Contains(results, r => r.MemberNames.Contains("Reasoning"));
    }

    [Fact]
    public void QualificationResult_NoGoWithReasoning_PassesValidation()
    {
        var result = new QualificationResult
        {
            FitScore = 2,
            RiskScore = 9,
            Recommendation = Recommendation.NoGo,
            Reasoning = "Unrealistic timeline and vague scope"
        };

        Assert.True(TryValidate(result, out _));
    }

    [Fact]
    public void QualificationResult_GoWithEmptyReasoning_PassesValidation()
    {
        // Go recommendation does not require reasoning
        var result = new QualificationResult
        {
            FitScore = 8,
            RiskScore = 3,
            Recommendation = Recommendation.Go
        };

        Assert.True(TryValidate(result, out _));
    }

    // --- Parameterized deserialization round-trip tests ---

    [Theory]
    [InlineData("""{"clientName":"X","budgetMin":100,"budgetMax":200,"classificationConfidence":0.5}""", "X", 100, 200)]
    [InlineData("""{"clientName":"","budgetMin":0,"budgetMax":0,"classificationConfidence":0.0}""", "", 0, 0)]
    public void OpportunityRecord_ParameterizedDeserialization(string json, string expectedName, int expectedMin, int expectedMax)
    {
        var record = JsonSerializer.Deserialize<OpportunityRecord>(json, PipelineJsonOptions.Default)!;

        Assert.Equal(expectedName, record.ClientName);
        Assert.Equal(expectedMin, record.BudgetMin);
        Assert.Equal(expectedMax, record.BudgetMax);
    }

    [Theory]
    [InlineData("""{"fitScore":1,"riskScore":10,"recommendation":"NoGo","reasoning":"Bad fit"}""", 1, 10, Recommendation.NoGo)]
    [InlineData("""{"fitScore":10,"riskScore":1,"recommendation":"Go","reasoning":"Perfect"}""", 10, 1, Recommendation.Go)]
    public void QualificationResult_ParameterizedDeserialization(string json, int expectedFit, int expectedRisk, Recommendation expectedRec)
    {
        var result = JsonSerializer.Deserialize<QualificationResult>(json, PipelineJsonOptions.Default)!;

        Assert.Equal(expectedFit, result.FitScore);
        Assert.Equal(expectedRisk, result.RiskScore);
        Assert.Equal(expectedRec, result.Recommendation);
    }

    [Theory]
    [InlineData("""{"overallStatus":"Clean","pricingConsistent":true}""", ReviewStatus.Clean, true, null)]
    [InlineData("""{"overallStatus":"IssuesFound","pricingConsistent":false,"targetAgent":"contract"}""", ReviewStatus.IssuesFound, false, "contract")]
    public void ReviewReport_ParameterizedDeserialization(string json, ReviewStatus expectedStatus, bool expectedPricing, string? expectedTarget)
    {
        var report = JsonSerializer.Deserialize<ReviewReport>(json, PipelineJsonOptions.Default)!;

        Assert.Equal(expectedStatus, report.OverallStatus);
        Assert.Equal(expectedPricing, report.PricingConsistent);
        Assert.Equal(expectedTarget, report.TargetAgent);
    }

    [Theory]
    [InlineData("""{"approved":true,"reviewerName":"Alice","feedback":"LGTM"}""", true, "Alice")]
    [InlineData("""{"approved":false,"reviewerName":"Bob","feedback":"Needs work"}""", false, "Bob")]
    public void ApprovalDecision_ParameterizedDeserialization(string json, bool expectedApproved, string expectedReviewer)
    {
        var decision = JsonSerializer.Deserialize<ApprovalDecision>(json, PipelineJsonOptions.Default)!;

        Assert.Equal(expectedApproved, decision.Approved);
        Assert.Equal(expectedReviewer, decision.ReviewerName);
    }

    // --- Deep nested object verification ---

    [Fact]
    public void ProposalDocument_RoundTrip_DeepNestedVerification()
    {
        var original = new ProposalDocument
        {
            ExecutiveSummary = "Cloud migration proposal for Acme Corp",
            Scope = "Migrate 14 legacy .NET applications to Azure",
            Deliverables =
            [
                new Deliverable { Name = "Assessment Report", Description = "Full technical assessment", DueDate = new DateTime(2025, 6, 1) },
                new Deliverable { Name = "Migration Plan", Description = "Phased migration roadmap", DueDate = new DateTime(2025, 7, 1) }
            ],
            Milestones =
            [
                new Milestone { Name = "Phase 1", Description = "Planning complete", DueDate = new DateTime(2025, 6, 15) },
                new Milestone { Name = "Phase 2", Description = "First 5 apps migrated", DueDate = new DateTime(2025, 9, 30) }
            ],
            PricingBreakdown =
            [
                new PricingLine { Role = "Architect", Rate = 275m, Hours = 1920, Subtotal = 528_000m },
                new PricingLine { Role = "Senior Developer", Rate = 225m, Hours = 3840, Subtotal = 864_000m }
            ],
            TotalPrice = 1_392_000m,
            EngagementType = EngagementType.StaffAugmentation
        };

        var json = JsonSerializer.Serialize(original, PipelineJsonOptions.Indented);
        var deserialized = JsonSerializer.Deserialize<ProposalDocument>(json, PipelineJsonOptions.Default)!;

        // Verify all nested objects, not just counts
        Assert.Equal(2, deserialized.Deliverables.Count);
        Assert.Equal("Assessment Report", deserialized.Deliverables[0].Name);
        Assert.Equal("Full technical assessment", deserialized.Deliverables[0].Description);
        Assert.Equal(new DateTime(2025, 6, 1), deserialized.Deliverables[0].DueDate);
        Assert.Equal("Migration Plan", deserialized.Deliverables[1].Name);

        Assert.Equal(2, deserialized.Milestones.Count);
        Assert.Equal("Phase 1", deserialized.Milestones[0].Name);
        Assert.Equal("Phase 2", deserialized.Milestones[1].Name);

        Assert.Equal(2, deserialized.PricingBreakdown.Count);
        Assert.Equal(275m, deserialized.PricingBreakdown[0].Rate);
        Assert.Equal(1920, deserialized.PricingBreakdown[0].Hours);
        Assert.Equal(528_000m, deserialized.PricingBreakdown[0].Subtotal);
        Assert.Equal("Senior Developer", deserialized.PricingBreakdown[1].Role);

        Assert.Equal(1_392_000m, deserialized.TotalPrice);
        Assert.Equal(EngagementType.StaffAugmentation, deserialized.EngagementType);
    }

    [Fact]
    public void ReviewReport_RoundTrip_DeepNestedVerification()
    {
        var original = new ReviewReport
        {
            OverallStatus = ReviewStatus.IssuesFound,
            Issues =
            [
                new ReviewIssue { Severity = IssueSeverity.Critical, Description = "Liability cap below threshold", Recommendation = "Increase to match contract value" },
                new ReviewIssue { Severity = IssueSeverity.Medium, Description = "Missing data protection clause", Recommendation = "Add GDPR clause for EU data" }
            ],
            PricingConsistent = false,
            RequirementsCovered =
            [
                new RequirementCheck { Requirement = "CI/CD Pipeline", Covered = true, Notes = "Addressed in Phase 2" },
                new RequirementCheck { Requirement = "Kubernetes", Covered = true, Notes = "AKS cluster provisioning included" },
                new RequirementCheck { Requirement = "SQL-to-Cosmos Migration", Covered = false, Notes = "Not explicitly addressed" }
            ],
            TargetAgent = "contract"
        };

        var json = JsonSerializer.Serialize(original, PipelineJsonOptions.Indented);
        var deserialized = JsonSerializer.Deserialize<ReviewReport>(json, PipelineJsonOptions.Default)!;

        Assert.Equal(2, deserialized.Issues.Count);
        Assert.Equal(IssueSeverity.Critical, deserialized.Issues[0].Severity);
        Assert.Equal("Liability cap below threshold", deserialized.Issues[0].Description);
        Assert.Equal(IssueSeverity.Medium, deserialized.Issues[1].Severity);

        Assert.Equal(3, deserialized.RequirementsCovered.Count);
        Assert.True(deserialized.RequirementsCovered[0].Covered);
        Assert.False(deserialized.RequirementsCovered[2].Covered);
        Assert.Equal("Not explicitly addressed", deserialized.RequirementsCovered[2].Notes);

        Assert.Equal("contract", deserialized.TargetAgent);
    }

    [Fact]
    public void ContractDocument_RoundTrip_AllFieldsVerified()
    {
        var original = new ContractDocument
        {
            ContractText = "This Master Services Agreement is entered into...",
            StandardClauses = ["IP-001", "LIA-001", "TERM-001", "PAY-001", "CONF-001"],
            CustomClauses = ["SA-001", "SA-002", "SA-003"],
            EffectiveDate = new DateTime(2025, 5, 1),
            TerminationTerms = "Either party may terminate with 30 days written notice",
            LiabilityCap = 1_500_000m,
            PaymentTerms = "Net 30 from invoice date"
        };

        var json = JsonSerializer.Serialize(original, PipelineJsonOptions.Indented);
        var deserialized = JsonSerializer.Deserialize<ContractDocument>(json, PipelineJsonOptions.Default)!;

        Assert.Equal(original.ContractText, deserialized.ContractText);
        Assert.Equal(5, deserialized.StandardClauses.Count);
        Assert.Equal("IP-001", deserialized.StandardClauses[0]);
        Assert.Equal("CONF-001", deserialized.StandardClauses[4]);
        Assert.Equal(3, deserialized.CustomClauses.Count);
        Assert.Equal(new DateTime(2025, 5, 1), deserialized.EffectiveDate);
        Assert.Contains("30 days", deserialized.TerminationTerms);
        Assert.Equal(1_500_000m, deserialized.LiabilityCap);
        Assert.Equal("Net 30 from invoice date", deserialized.PaymentTerms);
    }
}
