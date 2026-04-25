using System;

namespace MaNoir.Core.Contracts.Models.Mesh;

/// <summary>
/// Describes the current health of an internet connection.
/// </summary>
public enum ConnectionStatus
{
    /// <summary>
    /// The connection is available and healthy.
    /// </summary>
    Up,
    /// <summary>
    /// The connection is restarting.
    /// </summary>
    Restarting,
    /// <summary>
    /// The connection is degraded or unstable.
    /// </summary>
    Failing,
    /// <summary>
    /// The connection is unavailable.
    /// </summary>
    Down
}

/// <summary>
/// Represents a status refresh payload emitted for an internet connection.
/// </summary>
public sealed class InternetConnectionStatusRefresh
{
    /// <summary>
    /// Gets or sets the connection identifier.
    /// </summary>
    public string ConnectionId { get; set; }
    /// <summary>
    /// Gets or sets the connection technology or provider type.
    /// </summary>
    public string ConnectionType { get; set; }
    /// <summary>
    /// Gets or sets the current connection health.
    /// </summary>
    public ConnectionStatus Status { get; set; }
    /// <summary>
    /// Gets or sets the theoretical download bandwidth.
    /// </summary>
    public long DownloadBandwith { get; set; }
    /// <summary>
    /// Gets or sets the theoretical upload bandwidth.
    /// </summary>
    public long UploadBandwith { get; set; }
    /// <summary>
    /// Gets or sets the currently used download bandwidth.
    /// </summary>
    public long UsedDownloadBandwith { get; set; }
    /// <summary>
    /// Gets or sets the currently used upload bandwidth.
    /// </summary>
    public long UsedUploadBandwith { get; set; }
    /// <summary>
    /// Gets or sets the provider or diagnostic message.
    /// </summary>
    public string Message { get; set; }
    /// <summary>
    /// Gets or sets the detected Wi-Fi SSIDs when applicable.
    /// </summary>
    public string[] Ssids { get; set; }
}

/// <summary>
/// Represents the persisted state of an internet connection within the mesh.
/// </summary>
public sealed class InternetConnection
{
    /// <summary>
    /// Gets or sets the connection identifier.
    /// </summary>
    public string Id { get; set; }
    /// <summary>
    /// Gets or sets the connection technology or provider type.
    /// </summary>
    public string ConnectionType { get; set; }
    /// <summary>
    /// Gets or sets the current connection health.
    /// </summary>
    public ConnectionStatus Status { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether this connection is the primary one.
    /// </summary>
    public bool IsMain { get; set; }
    /// <summary>
    /// Gets or sets the theoretical download bandwidth.
    /// </summary>
    public long DownloadBandwith { get; set; }
    /// <summary>
    /// Gets or sets the theoretical upload bandwidth.
    /// </summary>
    public long UploadBandwith { get; set; }
    /// <summary>
    /// Gets or sets the currently used download bandwidth.
    /// </summary>
    public long UsedDownloadBandwith { get; set; }
    /// <summary>
    /// Gets or sets the currently used upload bandwidth.
    /// </summary>
    public long UsedUploadBandwith { get; set; }
    /// <summary>
    /// Gets or sets the last diagnostic message associated with the connection.
    /// </summary>
    public string LastMessage { get; set; }
    /// <summary>
    /// Gets or sets the last refresh timestamp.
    /// </summary>
    public DateTimeOffset LastUpdate { get; set; }
}