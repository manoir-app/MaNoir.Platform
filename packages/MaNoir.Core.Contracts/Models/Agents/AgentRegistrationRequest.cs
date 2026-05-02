using System.Collections.Generic;

namespace MaNoir.Core.Contracts.Models.Agents;

/// <summary>
/// Registers or refreshes the static identity of one agent.
/// </summary>
public sealed class AgentRegistrationRequest
{
    /// <summary>
    /// Gets or sets the logical agent identifier.
    /// </summary>
    public string AgentId { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the mesh identifier.
    /// </summary>
    public string MeshId { get; set; }

    /// <summary>
    /// Gets or sets the runtime version.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Gets or sets the declared capabilities.
    /// </summary>
    public List<string> Capabilities { get; set; }

    /// <summary>
    /// Gets or sets the current runtime state.
    /// </summary>
    public AgentState State { get; set; }

    /// <summary>
    /// Gets or sets an optional status message.
    /// </summary>
    public string StatusMessage { get; set; }
}