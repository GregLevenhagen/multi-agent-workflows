using Microsoft.Agents.AI;

namespace SalesToSignature.Agents.Agents;

/// <summary>
/// Common interface for all agent factories in the pipeline.
/// Enables DI registration and test stubbing of individual agents.
/// </summary>
public interface IAgentFactory
{
    /// <summary>Creates a configured <see cref="ChatClientAgent"/> ready for use in the handoff workflow.</summary>
    ChatClientAgent Create();

    /// <summary>
    /// Pre-flight validation check for the agent factory.
    /// Verifies that required dependencies (data files, tools, configuration) are available.
    /// Returns a list of error messages; empty list means the factory is ready.
    /// </summary>
    IReadOnlyList<string> Validate() => [];
}
