# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added
- 7-agent handoff workflow: Coordinator, Intake, Qualification, Proposal, Contract, Review, Approval
- Typed data models (C# records) for all pipeline stages: OpportunityRecord, QualificationResult, ProposalDocument, ContractDocument, ReviewReport, ApprovalDecision
- 3 sample RFP documents exercising all demo paths (happy path, no-go, review loop)
- SOW templates for 4 engagement types, MSA/NDA legal templates, clause library, rate cards
- Function tools: DocumentParser, TemplateLookup, PricingCalculator, LegalTemplateLookup, ClauseLibrary, ApproveContract
- Safety middleware: Prompt Shields with Spotlighting, Groundedness Validator, Content Safety Filter
- OpenTelemetry tracing with OTLP + console exporters, metrics (counter + histogram)
- Handoff orchestration via PipelineBuilder using AgentWorkflowBuilder
- Program.cs with DI, health checks, NDJSON streaming /responses endpoint
- Dockerfile with multi-stage build, health check, docker-compose.yml
- Bicep IaC: AI Foundry Hub/Project, ACR, App Insights, Content Safety, Key Vault, Storage, Managed Identity
- azd template with dev/staging/prod parameter files
- 212 tests: unit, integration, endpoint, edge case, safety middleware
- README, CONTRIBUTING, LICENSE, architecture docs, demo walkthrough
