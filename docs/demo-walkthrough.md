# Demo Walkthrough: Sales-to-Signature Pipeline

A 20-minute presenter guide for demonstrating the multi-agent pipeline.

## Setup

Before the demo:
1. Ensure Azure resources are provisioned (`azd up`)
2. Set the endpoint variable for convenience:
   ```bash
   # Local dev
   export ENDPOINT=http://localhost:8080
   # Deployed (replace with your hosted agent URL)
   # export ENDPOINT=https://<your-agent>.azurewebsites.net
   ```
3. Verify the agent is running (`curl $ENDPOINT/`)
4. Open VS Code with the Foundry extension for trace visualization
5. Have all 3 RFP files ready in tabs

## Act 1: Introduction (3 minutes)

**Show:** The Mermaid architecture diagram from `docs/architecture.md`

**Narrate:**
> "We've built a consulting firm's entire sales pipeline as 7 coordinated AI agents.
> When an RFP arrives, it flows through Intake, Qualification, Proposal, Contract,
> Review, and Approval — each agent specialized for its role.
> Let me show you three scenarios that exercise different paths."

## Act 2: Happy Path — Acme Cloud Migration (5 minutes)

**File:** `data/rfps/acme-cloud-migration-rfp.md`

```bash
curl -X POST $ENDPOINT/responses \
  -H "Content-Type: application/json" \
  -d @- << 'EOF'
{"input": "$(cat data/rfps/acme-cloud-migration-rfp.md)"}
EOF
```

**Expected Flow:**
1. **Coordinator** → routes to Intake
2. **Intake** → extracts: Acme Corp, Staff Augmentation, $500K-$750K, 12 months
3. **Qualification** → Fit: 9/10, Risk: 2/10, **Go**
4. **Proposal** → Drafts SOW with 5 roles, calculates pricing from rate cards
5. **Contract** → Generates MSA with staff aug-specific clauses
6. **Review** → **Clean** — pricing consistent, requirements covered
7. **Approval** → Presents summary, awaits human sign-off

**Show:** OpenTelemetry trace in VS Code Foundry visualizer — highlight the span waterfall

## Act 3: No-Go Path — Initech Advisory (5 minutes)

**File:** `data/rfps/initech-advisory-rfp.md`

```bash
curl -X POST $ENDPOINT/responses \
  -H "Content-Type: application/json" \
  -d @- << 'EOF'
{"input": "$(cat data/rfps/initech-advisory-rfp.md)"}
EOF
```

**Expected Flow:**
1. **Coordinator** → routes to Intake
2. **Intake** → extracts: Initech LLC, Advisory, $200K-$350K, 3 weeks
3. **Qualification** → Flags deal-breakers:
   - 3-week timeline for comprehensive assessment (unrealistic)
   - No CTO or technical decision-maker
   - Vague scope ("tell us what to do")
   - Risk: 9/10, **No-Go**
4. **Coordinator** → Reports rejection

**Narrate:**
> "Notice the pipeline stopped at Qualification. The agent detected three deal-breakers
> — all grounded in the actual RFP data, not hallucinated. This is where the
> Groundedness validator ensures the agent's reasoning is tied to source material."

## Act 4: Review Rejection Loop — Globex Data Platform (5 minutes)

**File:** `data/rfps/globex-data-platform-rfp.md`

```bash
curl -X POST $ENDPOINT/responses \
  -H "Content-Type: application/json" \
  -d @- << 'EOF'
{"input": "$(cat data/rfps/globex-data-platform-rfp.md)"}
EOF
```

**Expected Flow:**
1. **Intake** → extracts: Globex International, Fixed Bid, $1.2M-$1.5M, 18 months
2. **Qualification** → Fit: 7/10, Risk: 5/10, **Go** (complex but viable)
3. **Proposal** → Drafts SOW with 6 phases, milestone-based pricing
4. **Contract** → Generates MSA with fixed-bid clauses (change orders, warranties)
5. **Review** → **Issues Found** — flags missing data sovereignty clause, pricing inconsistency
6. **Contract** → Regenerates with corrections
7. **Review** → **Clean** on re-review
8. **Approval** → Presents updated summary

**Show:** The trace showing the review → contract → review loop — highlight the re-routing

## Act 5: Safety Features (2 minutes)

**Narrate:**
> "Throughout all three scenarios, safety middleware was active:
> - **Prompt Shields** screened every RFP for adversarial prompt injection
> - **Spotlighting** wrapped untrusted document content with delimiters
> - **Groundedness validation** ensured agent responses stayed true to source data
> - **Content Safety** filtered across hate, self-harm, sexual, and violence categories"

**Show:** OTel spans with `shield.*`, `groundedness.*`, `content_safety.*` tags

## Act 6: Architecture Deep Dive (Optional, 3 minutes)

Walk through the code:
1. `Orchestration/PipelineBuilder.cs` — the handoff graph
2. `Agents/AgentInstructions.cs` — a system prompt example
3. `Tools/PricingCalculator.cs` — how agents use function tools
4. `Safety/PromptShieldMiddleware.cs` — the safety layer

## Q&A Tips

**"Can agents call each other directly?"**
> No — the Handoff pattern means only the framework routes between agents.
> Each agent requests a handoff by calling a tool, and the framework decides the routing.

**"What happens if an agent hallucinates?"**
> The Groundedness validator catches this. It compares the agent's response against
> the source document and flags any ungrounded content.

**"How does the human approval work?"**
> The ApproveContract tool triggers a `ToolApprovalRequestContent` which pauses the
> workflow and surfaces the decision in the VS Code / portal UI.

## Troubleshooting

### `IChatClient not configured` error
The pipeline requires an LLM connection. For local dev:
1. Install `Microsoft.Extensions.AI.AzureAIInference` NuGet package
2. Set `AZURE_AI_PROJECT_ENDPOINT` to your Foundry project endpoint
3. Uncomment the `ChatCompletionsClient` registration in `Program.cs`

When deployed as a Foundry hosted agent, `IChatClient` is injected automatically.

### `dotnet build` fails with missing packages
Two packages referenced in the PRD are not yet on NuGet.org:
- `Azure.AI.AgentServer.AgentFramework`
- `Azure.AI.AgentServer.Core`

These are available when deployed through the Foundry agent hosting environment.

### Tests fail with `Could not find repo root`
Tests locate data files by walking up from `AppContext.BaseDirectory` looking for `global.json`.
Ensure you're running tests from within the repo: `dotnet test src/SalesToSignature.sln`

### Content Safety features not working
Set both `AZURE_CONTENT_SAFETY_ENDPOINT` and `AZURE_CONTENT_SAFETY_KEY` environment variables.
Without these, safety middleware is skipped (graceful degradation — pipeline still runs).

### OpenTelemetry traces not appearing
- **VS Code visualizer**: Ensure the Foundry extension is installed and OTLP endpoint is `http://localhost:4318`
- **Application Insights**: Set `APPLICATIONINSIGHTS_CONNECTION_STRING` and check the `OTEL_EXPORTER_OTLP_ENDPOINT` env var
- **Console output**: Set `OTEL_CONSOLE_EXPORT=true` for local debugging

### Docker build issues
The Dockerfile expects build context to be `src/SalesToSignature.Agents/`:
```bash
docker build --platform linux/amd64 -t sales-to-signature src/SalesToSignature.Agents/
```
Data files are not baked into the image — mount at runtime:
```bash
docker run -p 8080:8080 -v $(pwd)/data:/app/data sales-to-signature
```
