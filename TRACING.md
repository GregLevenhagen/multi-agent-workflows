# Tracing Implementation Summary

## Overview
Comprehensive distributed tracing has been added to the Sales-to-Signature multi-agent pipeline using OpenTelemetry. The implementation provides end-to-end observability across all agent stages, safety checks, and orchestration layers.

## Architecture

### OpenTelemetry Configuration
- **Service**: SalesToSignature.Agents v1.0.0
- **Sampling**: AlwaysOnSampler (100% trace capture)
- **Exporters**:
  - OTLP (OpenTelemetry Protocol) for Application Insights / VS Code Foundry visualizer
  - Console exporter for local debugging (when `OTEL_CONSOLE_EXPORT=true`)
- **Configuration via environment variables**:
  - `OTEL_EXPORTER_OTLP_ENDPOINT`: OTLP collector endpoint (default: http://localhost:4318)
  - `OTEL_CONSOLE_EXPORT`: Enable console debugging (set to "true")
  - `APPLICATIONINSIGHTS_CONNECTION_STRING`: Application Insights integration

## Trace Spans

### 1. Pipeline Request Root Span (`pipeline.request`)
**Location**: `Program.cs` - `/responses` POST endpoint
**Duration**: Full request lifecycle
**Tags**:
- `http.method`: POST
- `http.url`: Request path
- `pipeline.input_length`: Length of RFP text
- `pipeline.outcome`: completed | cancelled | error | rejected
- `pipeline.event_count`: Number of streaming events
- `pipeline.duration_ms`: Total execution time
- `pipeline.error`: Error description (if failed)
- `pipeline.error_type`: Exception type (if failed)
- `pipeline.error_details`: Exception message (if failed)

### 2. Safety Checks Span (`safety.prompt_shield`)
**Location**: `Program.cs` - Safety middleware invocation
**Timing**: Prompt Shield attack detection
**Tags**:
- `safety.attack_detected`: boolean
- `safety.attack_type`: Type of attack (if detected)
- `safety.confidence`: Confidence score (if detected)
- `safety.error`: Error type (if check failed)

**Metrics Recorded**:
- `safety.check.duration`: Histogram in milliseconds
- `safety.checks`: Counter with outcome tags (allowed | blocked)

### 3. Pipeline Build Spans
**Location**: `Orchestration/PipelineBuilder.cs`

#### 3a. Workflow Build (`pipeline.build_workflow`)
**Tags**:
- `pipeline.workflow_built`: boolean
- `pipeline.build_error`: Error type (if failed)
- `pipeline.error_details`: Error message (if failed)

#### 3b. Agent Creation (`pipeline.create_agents`)
**Tags**:
- `pipeline.agents_created`: Count (7)

#### 3c. Handoff Configuration (`pipeline.build_handoffs`)
**Tags**:
- `pipeline.handoff_count`: Number of handoffs (7)

### 4. Agent Factory Spans
**Location**: `Agents/*.cs` - All 7 agent factories
**Spans**:
- `agent.coordinator.create`
- `agent.intake.create`
- `agent.qualification.create`
- `agent.proposal.create`
- `agent.contract.create`
- `agent.review.create`
- `agent.approval.create`

**Tags (common to all)**:
- `agent.name`: Agent identifier
- `agent.description`: Agent description
- `agent.tool_count`: Number of tools (for agents with tools)

### 5. Streaming Events Span (`pipeline.stream`)
**Location**: `Program.cs` - Event streaming loop
**Tags**:
- `pipeline.events_streamed`: Count of NDJSON events sent

### 6. Existing Safety Validator Span (`Groundedness.Validate`)
**Location**: `Safety/GroundednessValidator.cs`
**Tags**:
- `groundedness.is_grounded`: boolean
- `groundedness.ungrounded_segments`: Count
- `groundedness.fallback`: boolean (fallback result)

## Metrics

### Counters

#### `pipeline.requests`
- **Unit**: requests
- **Tags**: `pipeline.outcome` (completed | cancelled | error | rejected)
- **Usage**: Track pipeline outcomes and success rates

#### `safety.checks`
- **Unit**: checks
- **Tags**: 
  - `safety.check.type` (prompt_shield | groundedness | content_safety)
  - `safety.check.outcome` (allowed | blocked | fallback)
- **Usage**: Monitor security check efficiency

### Histograms

#### `agent.duration`
- **Unit**: milliseconds
- **Tags**: `pipeline.stage` (agent name or full_pipeline)
- **Usage**: Track agent execution performance

#### `safety.check.duration`
- **Unit**: milliseconds
- **Tags**: `safety.check.type` (prompt_shield | groundedness | content_safety)
- **Usage**: Monitor security check latency

## Resource Attributes

Every trace is enriched with:
- `service.name`: SalesToSignature.Agents
- `service.version`: Assembly version
- `service.namespace`: sales-to-signature
- `deployment.environment`: Development/Staging/Production

## Integration with Visualization Tools

### VS Code AI Toolkit / Foundry Visualizer
Traces are exported via OTLP to support:
- Multi-agent workflow visualization
- Span tree inspection
- Trace filtering by outcome/duration
- Real-time monitoring

### Application Insights
When `APPLICATIONINSIGHTS_CONNECTION_STRING` is configured:
- Traces appear in Application Insights portal
- Cross-service correlation
- Performance analytics and alerting

### Local Development
Set `OTEL_CONSOLE_EXPORT=true` for console output:
```
Activity.OperationName: pipeline.request
    agent.duration: 1234 ms
    pipeline.outcome: completed
```

## Environment Variable Setup

```bash
# OTLP Exporter (Foundry/Local Collector)
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4318

# Application Insights
export APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=xxx;IngestionEndpoint=xxx

# Console debugging (local development only)
export OTEL_CONSOLE_EXPORT=true

# Application environment
export DOTNET_ENVIRONMENT=development
```

## Trace Query Examples

### Find all failed pipeline requests
```
Resource.attributes["service.name"] == "SalesToSignature.Agents" AND attributes["pipeline.outcome"] == "error"
```

### Find slow agent stages (>5 seconds)
```
attributes["agent.duration"] > 5000
```

### Find rejected requests (prompt injection)
```
attributes["pipeline.outcome"] == "rejected" AND attributes["safety.attack_detected"] == true
```

### Find all events from a specific pipeline run
```
trace_id == "specific-trace-id"
```

## Implementation Files Modified

1. **Program.cs**
   - Added root span for `/responses` endpoint
   - Integrated safety check tracing
   - Added streaming events span

2. **Orchestration/PipelineBuilder.cs**
   - Added workflow build tracing
   - Agent creation instrumentation
   - Handoff configuration spans

3. **Agents/*.cs** (7 files)
   - Added creation spans to all agent factories
   - Tagged with agent metadata

4. **Telemetry/TelemetrySetup.cs**
   - Already configured with OpenTelemetry
   - Existing activity source and metrics

## Best Practices for Extending Tracing

### Adding spans to new code
```csharp
using var activity = TelemetrySetup.AgentActivitySource.StartActivity("your.operation");
activity?.SetTag("key", value);
```

### Recording metrics
```csharp
TelemetrySetup.AgentDurationHistogram.Record(duration,
    new KeyValuePair<string, object?>("attribute", "value"));
```

### Error handling in spans
```csharp
try { /* operation */ }
catch (Exception ex)
{
    activity?.SetTag("error.type", ex.GetType().Name);
    activity?.SetTag("error.message", ex.Message);
}
```

## Performance Considerations

- **Sampling**: Currently set to 100% (AlwaysOnSampler). For high-volume production, consider probabilistic sampling
- **Memory**: Each active span uses ~5KB. Pipeline typically has 10-15 concurrent spans
- **Export latency**: OTLP batch export (default 10s or 512 spans) - non-blocking
- **No performance impact**: OpenTelemetry uses async export by default

## Future Enhancements

1. **Custom Attributes**:
   - Add RFP document metadata to root span
   - Track unique engagement IDs across trace

2. **Baggage Propagation**:
   - Share engagement context across service boundaries
   - Enable correlation with external APIs

3. **Profiling Integration**:
   - CPU/memory profiling during slow spans
   - Automatic bottleneck detection

4. **Alerts**:
   - High error rate on specific agents
   - Safety check rejection patterns
   - Pipeline SLA violations
