# PRD: Consulting Sales-to-Signature Multi-Agent Pipeline

## Introduction

Build a demo-able, open-source C# solution for the presentation **"Creating Multi-Agent Workflows with Azure AI Foundry and Microsoft Foundry Agent Service."** The solution demonstrates a consulting company's sales-to-signature pipeline where an RFP/lead flows through 7 coordinated agents using **Handoff orchestration** on **Microsoft Foundry Agent Service (Hosted Agents)**. The technical audience sees multi-agent coordination, safety guardrails, observability, and human-in-the-loop approval in action.

**Key technologies**: C# / .NET 9, Microsoft Agent Framework (convergence of AutoGen + Semantic Kernel), Foundry Agent Service Hosted Agents, Azure AI Content Safety (Prompt Shields, Groundedness, Content Safety), OpenTelemetry, and the Handoff orchestration pattern.

**NOT Azure Functions** — Microsoft now recommends Foundry Agent Service for agent hosting. The solution is containerized and deployed via Docker/ACR to Foundry.

## Goals

- Demonstrate a production-realistic multi-agent workflow using Microsoft's latest agent stack
- Show all 4 safety features: Prompt Shields + Spotlighting, Task Adherence / Groundedness, OpenTelemetry tracing, Content Safety filters
- Provide 3 demo paths: happy path (full pipeline to approval), no-go (early termination at qualification), and review rejection loop (routes back for fixes)
- Follow open-source conventions (README, LICENSE, CONTRIBUTING, IaC, .editorconfig)
- Be easily runnable locally (`dotnet run`) and deployable to Azure (`azd up`)

## User Stories

### US-001: Solution scaffolding and project structure
**Description:** As a developer cloning this repo, I want a well-structured .NET 9 solution so I can build and run locally immediately.

**Acceptance Criteria:**
- [ ] Create `src/SalesToSignature.sln` referencing both projects
- [ ] Create `src/SalesToSignature.Agents/SalesToSignature.Agents.csproj` targeting `net9.0` with NuGet packages: `Microsoft.Agents.AI` (1.1.0+), `Microsoft.Agents.AI.Workflows` (1.1.0+), `Azure.AI.AgentServer.AgentFramework`, `Azure.AI.AgentServer.Core`, `Azure.AI.Projects` (2.0.0+), `Azure.AI.ContentSafety` (1.0.0+), `Azure.Identity`, `OpenTelemetry` (1.12.0+), `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Exporter.Console`, `System.Diagnostics.DiagnosticSource`
- [ ] Create `src/SalesToSignature.Tests/SalesToSignature.Tests.csproj` targeting `net9.0` with xUnit, Moq, and project reference to Agents project
- [ ] Create `global.json` pinning .NET 9 SDK
- [ ] Create `src/SalesToSignature.Agents/Dockerfile` using multi-stage build: `mcr.microsoft.com/dotnet/sdk:9.0` for build, `mcr.microsoft.com/dotnet/aspnet:9.0` for runtime, exposing port 8080, with `--platform linux/amd64` note in comments
- [ ] Create `src/SalesToSignature.Agents/agent.yaml` Foundry manifest with environment variable placeholders for AZURE_AI_PROJECT_ENDPOINT, AZURE_AI_MODEL_DEPLOYMENT_NAME
- [ ] Create `.env.example` documenting all required environment variables: AZURE_AI_PROJECT_ENDPOINT, AZURE_AI_MODEL_DEPLOYMENT_NAME, AZURE_CONTENT_SAFETY_ENDPOINT, AZURE_CONTENT_SAFETY_KEY, APPLICATIONINSIGHTS_CONNECTION_STRING, OTEL_CONSOLE_EXPORT (true/false)
- [ ] Create `src/SalesToSignature.Agents/Configuration/AgentSettings.cs` with strongly-typed settings: ProjectEndpoint, ModelDeploymentName, ContentSafetyEndpoint, ContentSafetyKey, AppInsightsConnectionString
- [ ] `dotnet build` succeeds with no errors
- [ ] `dotnet test` succeeds (even with no tests yet)

### US-002: Typed data models
**Description:** As a developer, I want strongly-typed C# data models for all pipeline stages so data flowing between agents is validated at compile time.

**Acceptance Criteria:**
- [ ] Create `Models/EngagementType.cs` — enum: `StaffAugmentation`, `FixedBid`, `TimeAndMaterials`, `Advisory`
- [ ] Create `Models/OpportunityRecord.cs` — properties: ClientName (string), EngagementType, BudgetMin (decimal), BudgetMax (decimal), TimelineStart (DateTime), TimelineEnd (DateTime), TechStack (List<string>), KeyRequirements (List<string>), RawDocumentText (string), ClassificationConfidence (double). All with `[JsonPropertyName]` attributes
- [ ] Create `Models/QualificationResult.cs` — properties: FitScore (int, 1-10), RiskScore (int, 1-10), RevenuePotential (decimal), RequiredSkills (List<string>), Risks (List<string>), DealBreakers (List<string>), Recommendation (enum: Go/NoGo), Reasoning (string)
- [ ] Create `Models/ProposalDocument.cs` — properties: ExecutiveSummary (string), Scope (string), Deliverables (List<Deliverable> with Name, Description, DueDate), Milestones (List<Milestone>), PricingBreakdown (List<PricingLine> with Role, Rate, Hours, Subtotal), TotalPrice (decimal), EngagementType
- [ ] Create `Models/ContractDocument.cs` — properties: ContractText (string), StandardClauses (List<string>), CustomClauses (List<string>), EffectiveDate (DateTime), TerminationTerms (string), LiabilityCap (decimal), PaymentTerms (string)
- [ ] Create `Models/ReviewReport.cs` — properties: OverallStatus (enum: Clean/IssuesFound), Issues (List<ReviewIssue> with Severity, Description, Recommendation), PricingConsistent (bool), RequirementsCovered (List<RequirementCheck> with Requirement, Covered bool), TargetAgent (string, nullable — "Contract" or "Proposal" for re-routing)
- [ ] Create `Models/ApprovalDecision.cs` — properties: Approved (bool), ReviewerName (string), Feedback (string), Timestamp (DateTime), ContractSummary (string)
- [ ] All models are JSON-serializable with `System.Text.Json`
- [ ] `dotnet build` passes

### US-003: Sample RFP documents
**Description:** As a presenter, I want realistic sample RFP documents so I can demonstrate the pipeline with compelling, varied scenarios that exercise different code paths.

**Acceptance Criteria:**
- [ ] Create `data/rfps/acme-cloud-migration-rfp.md` — ~800 words, Acme Corp (Fortune 500 manufacturer), staff augmentation engagement, Azure cloud migration of legacy .NET apps, budget $500K-$750K, 12-month timeline, need 3 senior .NET devs + 1 cloud architect + 1 DevOps engineer, specific requirements: CI/CD pipeline, Kubernetes, SQL-to-Cosmos migration. This RFP should sail through all stages to approval (happy path)
- [ ] Create `data/rfps/globex-data-platform-rfp.md` — ~1000 words, Globex International (mid-market fintech), fixed-bid engagement, real-time data analytics platform from scratch, budget $1.2M-$1.5M, 18-month timeline, requires Databricks + Snowflake + Power BI + real-time streaming, complex deliverables with 6+ milestones. This RFP should trigger review issues on first pass (review rejection loop path)
- [ ] Create `data/rfps/initech-advisory-rfp.md` — ~600 words, Initech LLC (50-employee startup), advisory/T&M engagement, digital transformation assessment, budget $200K-$350K but only 3-week timeline (unrealistic), vague requirements, no clear stakeholder. This RFP should trigger a no-go at qualification (deal-breakers: unrealistic timeline + vague scope)
- [ ] Each RFP reads like a real document a consulting firm would receive
- [ ] Each RFP contains enough detail for the Intake agent to extract structured data

### US-004: SOW templates, legal templates, and pricing data
**Description:** As a developer, I want reference data files that the pipeline's tools read from so agents can generate proposals and contracts grounded in real templates.

**Acceptance Criteria:**
- [ ] Create `data/templates/sow-template-staffaug.md` — SOW template for staff augmentation with sections: Executive Summary, Scope of Work, Team Composition, Timeline, Pricing, Terms
- [ ] Create `data/templates/sow-template-fixedbid.md` — SOW template for fixed-bid with sections: Executive Summary, Scope, Deliverables, Milestones, Acceptance Criteria, Pricing, Payment Schedule
- [ ] Create `data/templates/sow-template-tm.md` — SOW template for time-and-materials engagements
- [ ] Create `data/templates/sow-template-advisory.md` — SOW template for advisory engagements
- [ ] Create `data/legal/msa-template.md` — Master Services Agreement template with standard consulting terms (IP assignment, liability, indemnification, confidentiality, termination, governing law)
- [ ] Create `data/legal/nda-template.md` — Non-disclosure agreement template
- [ ] Create `data/legal/standard-clauses.json` — JSON array of 12+ clause objects with fields: id, name, text, category (one of: IP, Liability, Termination, Payment, Confidentiality, DataProtection, ForceMajeure, NonSolicitation)
- [ ] Create `data/legal/engagement-specific-clauses.json` — Additional clauses keyed by EngagementType
- [ ] Create `data/pricing/rate-cards.json` — Rate cards with 5 roles (Architect, SeniorDev, Developer, ProjectManager, QAEngineer), hourly and daily rates for each engagement type
- [ ] All JSON files are valid and parseable
- [ ] `dotnet build` passes

### US-005: Agent system prompts
**Description:** As a developer, I want detailed system prompts for each of the 7 agents so each agent has clear instructions tied to its role in the consulting pipeline.

**Acceptance Criteria:**
- [ ] Create `Agents/AgentInstructions.cs` as a static class with string constants for each agent
- [ ] `CoordinatorInstructions` — You are the coordinator for a consulting sales pipeline. When a new document arrives, hand off to the Intake agent. You manage the overall flow.
- [ ] `IntakeInstructions` — Parse RFP documents, classify engagement type (StaffAugmentation, FixedBid, TimeAndMaterials, Advisory), extract structured data into OpportunityRecord JSON. Use the DocumentParser tool. When done, hand off to the Qualification agent.
- [ ] `QualificationInstructions` — Score opportunity on fit (1-10), risk (1-10), revenue potential. Identify required skills. Flag deal-breakers (unrealistic timelines, vague scope, budget mismatches). Make go/no-go recommendation with reasoning grounded ONLY in the RFP data. If go, hand off to Proposal agent. If no-go, respond with detailed rejection reasoning.
- [ ] `ProposalInstructions` — Draft a Statement of Work using the TemplateLookup tool for the right template and PricingCalculator for pricing. Include executive summary, scope, deliverables, milestones, pricing breakdown. Content must match what the client actually requested. When done, hand off to Contract agent.
- [ ] `ContractInstructions` — Generate a contract from the SOW using LegalTemplateLookup for MSA/NDA templates and ClauseLibrary for standard and engagement-specific clauses. Produce a complete draft contract. When done, hand off to Review agent.
- [ ] `ReviewInstructions` — Cross-check the contract against the original RFP requirements. Validate pricing consistency between SOW and contract. Flag non-standard, missing, or risky clauses. If clean, hand off to Approval agent. If contract issues found, hand off to Contract agent with specific feedback. If scope/proposal issues found, hand off to Proposal agent with specific feedback.
- [ ] `ApprovalInstructions` — Present the contract summary and any review findings to the human reviewer. Use the ApproveContract tool (which requires human approval) to get sign-off. Report the final approval decision.
- [ ] Each prompt is at least 150 words with clear, specific instructions
- [ ] Each prompt explicitly tells the agent when and to whom to hand off
- [ ] `dotnet build` passes

### US-006: Agent factory classes
**Description:** As a developer, I want factory classes that create each agent with the correct instructions, tools, and configuration so the orchestration builder can wire them together.

**Acceptance Criteria:**
- [ ] Create `Agents/CoordinatorAgentFactory.cs` — creates a `ChatClientAgent` with CoordinatorInstructions, name "coordinator", description "Routes new documents to the pipeline"
- [ ] Create `Agents/IntakeAgentFactory.cs` — creates agent with IntakeInstructions, name "intake", tools: [DocumentParser]
- [ ] Create `Agents/QualificationAgentFactory.cs` — creates agent with QualificationInstructions, name "qualification"
- [ ] Create `Agents/ProposalAgentFactory.cs` — creates agent with ProposalInstructions, name "proposal", tools: [PricingCalculator, TemplateLookup]
- [ ] Create `Agents/ContractAgentFactory.cs` — creates agent with ContractInstructions, name "contract", tools: [LegalTemplateLookup, ClauseLibrary]
- [ ] Create `Agents/ReviewAgentFactory.cs` — creates agent with ReviewInstructions, name "review"
- [ ] Create `Agents/ApprovalAgentFactory.cs` — creates agent with ApprovalInstructions, name "approval", tools: [ApproveContract wrapped in ApprovalRequiredAIFunction]
- [ ] All factories accept `IChatClient` via constructor parameter
- [ ] All factories return `ChatClientAgent` with name and description set
- [ ] `dotnet build` passes

### US-007: Function tools (DocumentParser, TemplateLookup, PricingCalculator)
**Description:** As a developer, I want function tools that agents can call to parse documents, look up templates, and calculate pricing.

**Acceptance Criteria:**
- [ ] Create `Tools/DocumentParser.cs` — static method with `[Description("Parse and extract text from an RFP document")]` attribute, takes string documentText, returns cleaned/normalized text. Use `AIFunctionFactory.Create` pattern
- [ ] Create `Tools/TemplateLookup.cs` — static method with `[Description("Retrieve SOW template for a given engagement type")]`, takes string engagementType, reads from `data/templates/sow-template-{type}.md`, returns template text. Data path configurable via constructor or settings
- [ ] Create `Tools/PricingCalculator.cs` — static method with `[Description("Calculate project pricing based on engagement type, team roles, and duration in months")]`, reads rate cards from `data/pricing/rate-cards.json`, computes pricing breakdown by role, returns JSON pricing summary
- [ ] All tool methods use `[Description]` attributes on both the method and parameters
- [ ] All tools that read files use relative paths from a configurable data directory
- [ ] `dotnet build` passes
- [ ] Tests pass — unit test for PricingCalculator with known inputs producing expected output

### US-008: Function tools (LegalTemplateLookup, ClauseLibrary)
**Description:** As a developer, I want legal-focused function tools so the Contract agent can look up MSA/NDA templates and query the clause library.

**Acceptance Criteria:**
- [ ] Create `Tools/LegalTemplateLookup.cs` — static method with `[Description("Retrieve legal contract templates (MSA or NDA)")]`, takes string templateType ("msa" or "nda"), reads from `data/legal/{type}-template.md`, returns template text
- [ ] Create `Tools/ClauseLibrary.cs` — static method with `[Description("Look up contract clauses by category or engagement type")]`, takes optional string category and optional string engagementType, reads from `data/legal/standard-clauses.json` and `data/legal/engagement-specific-clauses.json`, filters and returns matching clauses as JSON
- [ ] Both tools use `[Description]` on methods and parameters
- [ ] Both tools read from configurable data directory
- [ ] `dotnet build` passes
- [ ] Tests pass — unit test for ClauseLibrary filtering by category

### US-009: Safety middleware — Prompt Shields with Spotlighting
**Description:** As a developer, I want Prompt Shield middleware so the Intake agent can screen untrusted RFP documents for adversarial prompt injection before processing.

**Acceptance Criteria:**
- [ ] Create `Safety/PromptShieldMiddleware.cs` with a class that accepts `ContentSafetyClient` (from `Azure.AI.ContentSafety` NuGet) via constructor
- [ ] Implement `AnalyzeDocumentAsync(string documentText)` method that calls the Prompt Shield API (`AnalyzeText` with jailbreak detection options)
- [ ] Detect both user prompt attacks and document attacks
- [ ] Return a `ShieldResult` record with properties: IsAttackDetected (bool), AttackType (string, nullable), Confidence (double)
- [ ] Implement Spotlighting: method `ApplySpotlighting(string documentText)` that wraps untrusted document content with delimiter markers (e.g., `<document>` tags) to indicate lower trust to the model
- [ ] Log shield results as OTel span attributes when an ActivitySource is available
- [ ] `dotnet build` passes
- [ ] Tests pass — unit test with mocked ContentSafetyClient verifying attack detection and spotlighting

### US-010: Safety middleware — Groundedness and Content Safety
**Description:** As a developer, I want Groundedness validation and Content Safety filtering so agents stay grounded in source data and harmful content is blocked.

**Acceptance Criteria:**
- [ ] Create `Safety/GroundednessValidator.cs` with a class that accepts `ContentSafetyClient` via constructor
- [ ] Implement `ValidateGroundednessAsync(string response, string groundingSources)` that calls the Groundedness Detection API
- [ ] Return a `GroundednessResult` record: IsGrounded (bool), UngroundedSegments (List<string>), Reasoning (string)
- [ ] Create `Safety/ContentSafetyFilter.cs` with a class that accepts `ContentSafetyClient` via constructor
- [ ] Implement `AnalyzeTextAsync(string text)` that calls the AnalyzeText API across all 4 categories (hate, violence, sexual, self-harm)
- [ ] Return a `SafetyResult` record: IsBlocked (bool), Categories (Dictionary<string, int> with severity scores per category)
- [ ] Configurable severity thresholds via AgentSettings
- [ ] `dotnet build` passes
- [ ] Tests pass — unit tests with mocked ContentSafetyClient for both groundedness and content safety

### US-011: OpenTelemetry setup
**Description:** As a presenter, I want end-to-end distributed tracing so I can demonstrate observability across all agents in Application Insights and the VS Code Foundry visualizer.

**Acceptance Criteria:**
- [ ] Create `Telemetry/TelemetrySetup.cs` with a static method `ConfigureOpenTelemetry(IServiceCollection services, AgentSettings settings)`
- [ ] Configure `TracerProvider` with ActivitySource name `"SalesToSignature.Agents"`
- [ ] Add `Microsoft.Agents.AI` as a trace source to capture framework-level spans automatically
- [ ] Add OTLP exporter when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set (points to Application Insights OTLP endpoint)
- [ ] Add Console exporter when `OTEL_CONSOLE_EXPORT=true` for local development
- [ ] Use `AlwaysOnSampler` to capture all traces
- [ ] Set resource builder with service name `"SalesToSignature.Agents"`
- [ ] Provide helper method `StartAgentSpan(string agentName)` that creates an Activity with attributes: `agent.name`, `agent.stage`
- [ ] `dotnet build` passes

### US-012: Handoff orchestration PipelineBuilder
**Description:** As a developer, I want the handoff workflow graph wired up so all 7 agents coordinate correctly with the right handoff rules.

**Acceptance Criteria:**
- [ ] Create `Orchestration/PipelineBuilder.cs` with a `BuildPipeline(IChatClient chatClient, ...)` method
- [ ] Create all 7 agents via their factory classes
- [ ] Build handoff workflow using `AgentWorkflowBuilder.CreateHandoffBuilderWith(coordinatorAgent)`
- [ ] Configure handoff rules: coordinator → [intake], intake → [qualification], qualification → [proposal, coordinator], proposal → [contract], contract ��� [review], review → [approval, contract, proposal]
- [ ] Approval agent uses `ApprovalRequiredAIFunction` wrapping the approve-contract tool
- [ ] Return the built workflow
- [ ] `dotnet build` passes

### US-013: Program.cs entry point with hosting adapter
**Description:** As a developer, I want the main entry point that wires DI, builds the workflow, and starts the hosting adapter on localhost:8080.

**Acceptance Criteria:**
- [ ] Create `Program.cs` that reads configuration from environment variables
- [ ] Register DI services: `IChatClient` (via `AIProjectClient` + `DefaultAzureCredential`), `ContentSafetyClient`, all tool classes, all safety middleware, AgentSettings
- [ ] Call `TelemetrySetup.ConfigureOpenTelemetry()` to configure OTel
- [ ] Build the handoff workflow via `PipelineBuilder.BuildPipeline()`
- [ ] Start the hosting adapter using `from_agent_framework(workflow).run()` pattern (C# equivalent via `Azure.AI.AgentServer.AgentFramework`) serving on port 8080
- [ ] Local testing: `dotnet run` starts HTTP server on `http://localhost:8080`
- [ ] POST to `http://localhost:8080/responses` with `{"input": "<RFP text>"}` triggers the pipeline
- [ ] `dotnet build` passes
- [ ] Docker build succeeds: `docker build --platform linux/amd64 -t sales-to-signature .` from the Agents project directory

### US-014: Unit tests for models, tools, and safety
**Description:** As a developer, I want unit tests validating data models, tool functions, and safety middleware so the pre-commit hook passes.

**Acceptance Criteria:**
- [ ] Create `Tests/Models/ModelSerializationTests.cs` — test JSON round-trip serialization for OpportunityRecord, QualificationResult, ProposalDocument, ContractDocument, ReviewReport, ApprovalDecision
- [ ] Create `Tests/Tools/PricingCalculatorTests.cs` — test pricing calculation with known role/hours/rate inputs
- [ ] Create `Tests/Tools/ClauseLibraryTests.cs` — test clause filtering by category and engagement type
- [ ] Create `Tests/Safety/PromptShieldTests.cs` — test with mocked ContentSafetyClient that attack detection returns correct ShieldResult
- [ ] Create `Tests/Safety/GroundednessValidatorTests.cs` — test with mocked client that ungrounded content is detected
- [ ] Create `Tests/Safety/ContentSafetyFilterTests.cs` — test with mocked client that harmful content is blocked
- [ ] All tests use xUnit and Moq
- [ ] `dotnet test` passes with all tests green

### US-015: Integration tests for pipeline paths
**Description:** As a developer, I want integration tests verifying the complete pipeline works with mocked LLM responses for all 3 demo paths.

**Acceptance Criteria:**
- [ ] Create `Tests/Integration/HappyPathTests.cs` — mock IChatClient returns predetermined responses for each agent stage. Acme RFP goes through intake → qualification (go) → proposal → contract → review (clean) → approval gate
- [ ] Create `Tests/Integration/NoGoPathTests.cs` — Initech RFP triggers qualification no-go, pipeline terminates with rejection reasoning
- [ ] Create `Tests/Integration/ReviewLoopTests.cs` — Globex RFP triggers review finding contract issues, routes back to Contract agent, re-review passes, routes to approval
- [ ] Mock responses are realistic enough to validate the handoff routing works correctly
- [ ] `dotnet test` passes with all integration tests green

### US-016: Open source documentation
**Description:** As a developer cloning this repo, I want comprehensive documentation so I can understand the architecture, set up my environment, and run the demo.

**Acceptance Criteria:**
- [ ] Create `README.md` with: project title and description, architecture diagram (Mermaid syntax showing 7 agents with handoff arrows, safety middleware, OTel), prerequisites (Azure subscription, .NET 9 SDK, Docker, Azure CLI, VS Code + Foundry extension), quick start (clone, .env setup, `dotnet run`, curl test), Azure deployment instructions (`azd up`), required Azure resources table, MIT license badge
- [ ] Create `CONTRIBUTING.md` with standard open-source contribution guidelines (fork, branch, PR, code style)
- [ ] Create `LICENSE` with MIT license text
- [ ] Create `.editorconfig` enforcing C# conventions: 4-space indent, UTF-8, newline at end of file, trim trailing whitespace
- [ ] Create `docs/architecture.md` with detailed Mermaid diagrams: workflow graph, safety middleware pipeline, OTel span hierarchy
- [ ] Create `docs/demo-walkthrough.md` with step-by-step presenter guide: 6 acts (architecture overview, code walkthrough, happy path demo, safety demo, no-go path, review loop), curl commands for each demo, expected outputs
- [ ] All documentation links and code examples are accurate
- [ ] `dotnet build` passes (unchanged, but verify docs don't break anything)

### US-017: Infrastructure as Code (Bicep + azd)
**Description:** As a developer, I want Bicep templates and an azd configuration so I can provision all required Azure resources with `azd up`.

**Acceptance Criteria:**
- [ ] Create `infra/main.bicep` orchestrating all modules with parameters: location, projectName, modelName (default gpt-4o)
- [ ] Create `infra/modules/foundry.bicep` — Foundry Resource (AIServices kind) + Foundry Project
- [ ] Create `infra/modules/acr.bicep` — Azure Container Registry (Basic tier)
- [ ] Create `infra/modules/model-deployment.bicep` — model deployment in Foundry
- [ ] Create `infra/modules/app-insights.bicep` — Application Insights + Log Analytics Workspace
- [ ] Create `infra/modules/content-safety.bicep` — Azure AI Content Safety resource
- [ ] Create `infra/modules/managed-identity.bicep` — User-Assigned Managed Identity with RBAC: Azure AI User on Foundry project, AcrPull on ACR
- [ ] Create `infra/parameters/dev.bicepparam` with sample parameter values
- [ ] Create `infra/azure.yaml` azd template definition
- [ ] All Bicep files have valid syntax (no parse errors)
- [ ] `dotnet build` passes

## Functional Requirements

- FR-1: The system must accept an RFP document (as text) via HTTP POST to `/responses` and process it through 7 coordinated agents
- FR-2: The Intake agent must classify the engagement type and extract structured opportunity data from the RFP
- FR-3: The Qualification agent must score the opportunity and recommend go/no-go based solely on RFP data
- FR-4: The Proposal agent must draft a SOW using templates and pricing data from the `data/` directory
- FR-5: The Contract agent must generate a contract using legal templates and clause libraries from the `data/` directory
- FR-6: The Review agent must cross-check the contract against the original RFP and flag issues
- FR-7: The Review agent must hand off to the Approval agent if clean, or back to Contract/Proposal agent if issues found
- FR-8: The Approval agent must pause for human approval via `ApprovalRequiredAIFunction` before finalizing
- FR-9: The Intake agent must screen incoming RFP documents through Prompt Shields with Spotlighting
- FR-10: The Qualification, Proposal, and Review agents must validate outputs through Groundedness Detection
- FR-11: All agent inputs and outputs must pass through Content Safety filters
- FR-12: All agent invocations must emit OpenTelemetry spans with agent name, stage, and token usage attributes
- FR-13: The system must run locally via `dotnet run` on port 8080 for demo purposes
- FR-14: The system must be deployable as a Hosted Agent to Foundry Agent Service via Docker container

## Non-Goals

- No Azure Functions — Foundry Agent Service Hosted Agents only
- No Semantic Kernel or AutoGen directly — only via Microsoft Agent Framework
- No UI or frontend — demo uses curl, Foundry Playground, and VS Code Foundry extension
- No database — all reference data is file-based (JSON, Markdown) in `data/` directory
- No real contract management system — this is a demo, not production software
- No email/notification integration
- No multi-tenant support
- No authentication on the local HTTP endpoint (Foundry handles auth in production)

## Technical Considerations

- **Hosting**: Foundry Agent Service Hosted Agents (containerized, deployed via ACR). See https://learn.microsoft.com/en-us/azure/foundry/agents/concepts/hosted-agents
- **Orchestration**: Handoff pattern from Microsoft Agent Framework. See https://learn.microsoft.com/en-us/agent-framework/workflows/orchestrations/handoff
- **Agent Framework**: Microsoft Agent Framework 1.x (convergence of AutoGen + Semantic Kernel). See https://learn.microsoft.com/en-us/agent-framework/overview/
- **Safety APIs**: Azure AI Content Safety — Prompt Shields, Groundedness Detection, Text Analysis. See https://learn.microsoft.com/en-us/azure/ai-services/content-safety/
- **NuGet packages**: `Microsoft.Agents.AI` (1.1.0+), `Microsoft.Agents.AI.Workflows`, `Azure.AI.AgentServer.AgentFramework`, `Azure.AI.AgentServer.Core`, `Azure.AI.Projects` (2.0.0+), `Azure.AI.ContentSafety`, `Azure.Identity`, `OpenTelemetry` (1.12.0+), `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Exporter.Console`
- **Docker**: Must build with `--platform linux/amd64` for Foundry Agent Service compatibility
- **Local testing**: Hosting adapter runs on `localhost:8080`, accepts POST `/responses` with `{"input": "..."}`
- **Deployment**: Via `azd ai agent init` + `azd up`, or VS Code Foundry extension "Deploy Hosted Agent" command

## Success Metrics

- `dotnet build` and `dotnet test` pass with zero errors
- All 3 demo paths execute correctly (happy path, no-go, review loop)
- Full pipeline traces visible in Application Insights with connected spans per agent
- Docker container builds and runs successfully
- Complete demo can be walked through in under 20 minutes
- A developer unfamiliar with the project can clone, configure .env, run `dotnet run`, and test within 10 minutes

## Open Questions

- Exact NuGet package versions for `Azure.AI.AgentServer.AgentFramework` and `Azure.AI.AgentServer.Core` (currently in beta — verify latest on nuget.org before implementing)
- Whether the C# Handoff builder API uses `WithHandoffs` or `AddHandoff` (verify against latest Agent Framework 1.x docs)
- Application Insights OTLP endpoint format for the OTel exporter configuration
- Whether `ApprovalRequiredAIFunction` is available in C# Agent Framework or if the approval pattern differs from the Python SDK
