using MaNoir.Core.Contracts.Models.Mesh;

namespace MaNoir.Core.Mesh;

/// <summary>
/// Describes one persisted local internet connection refresh.
/// </summary>
public sealed class InternetConnectionMonitoringResult
{
    /// <summary>
    /// Gets or sets the mesh snapshot after the refresh was persisted.
    /// </summary>
    public AutomationMesh Mesh { get; set; }

    /// <summary>
    /// Gets or sets the internet connection entry that was created or updated.
    /// </summary>
    public InternetConnection Connection { get; set; }

    /// <summary>
    /// Gets or sets whether the aggregated mesh status changed because of this refresh.
    /// </summary>
    public bool MeshStatusChanged { get; set; }
}