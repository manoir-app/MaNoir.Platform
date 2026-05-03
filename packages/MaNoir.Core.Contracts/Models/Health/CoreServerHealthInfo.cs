using System;

namespace MaNoir.Core.Contracts.Models.Health;

/// <summary>
/// Represents the public server information exposed before authentication.
/// </summary>
public sealed class CoreServerHealthInfo
{
    /// <summary>
    /// Gets or sets the display name of the local mesh.
    /// </summary>
    public string MeshName { get; set; }

    /// <summary>
    /// Gets or sets the public or detected domain name of the local server.
    /// </summary>
    public string DomainName { get; set; }

    /// <summary>
    /// Gets or sets the version of the running Admin UI host.
    /// </summary>
    public string AdminUiVersion { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp at which the current process started.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the current uptime in seconds.
    /// </summary>
    public long UptimeSeconds { get; set; }
}