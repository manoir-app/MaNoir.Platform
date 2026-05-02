namespace MaNoir.Core.Contracts.Models.Agents;

/// <summary>
/// Updates the liveness state of one registered agent.
/// </summary>
public sealed class AgentHeartbeatRequest
{
    /// <summary>
    /// Gets or sets the logical agent identifier.
    /// </summary>
    public string AgentId { get; set; }

    /// <summary>
    /// Gets or sets the mesh identifier.
    /// </summary>
    public string MeshId { get; set; }

    /// <summary>
    /// Gets or sets the current runtime state.
    /// </summary>
    public AgentState State { get; set; }

    /// <summary>
    /// Gets or sets an optional status message.
    /// </summary>
    public string StatusMessage { get; set; }
}