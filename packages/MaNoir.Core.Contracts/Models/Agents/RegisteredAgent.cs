using System;
using System.Collections.Generic;

namespace MaNoir.Core.Contracts.Models.Agents;

/// <summary>
/// Describes one agent currently known by the platform registry.
/// </summary>
public sealed class RegisteredAgent
{
    /// <summary>
    /// Gets or sets the persistent identifier of the registry entry.
    /// </summary>
    public string Id { get; set; }

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

    /// <summary>
    /// Gets or sets the first registration timestamp in UTC.
    /// </summary>
    public DateTimeOffset RegisteredAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the last heartbeat timestamp in UTC.
    /// </summary>
    public DateTimeOffset LastHeartbeatUtc { get; set; }

    /// <summary>
    /// Gets or sets the last registry update timestamp in UTC.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }
}