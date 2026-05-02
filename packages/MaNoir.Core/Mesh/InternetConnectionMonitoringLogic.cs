using MaNoir.Core.Contracts.Models.Mesh;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Mesh;

/// <summary>
/// Persists periodic WAN health refreshes into the local automation mesh.
/// </summary>
/// <remarks>
/// <para>Example:</para>
/// <code>
/// InternetConnectionMonitoringLogic logic = new InternetConnectionMonitoringLogic();
/// InternetConnectionMonitoringResult result = await logic.RefreshLocalConnectionAsync(new InternetConnectionStatusRefresh()
/// {
///     ConnectionId = "wan-primary",
///     StatusCode = "ok"
/// }, Environment.MachineName, "https://core.local/api/graph/", cancellationToken);
/// </code>
/// </remarks>
public sealed class InternetConnectionMonitoringLogic
{
    private readonly AutomationMeshLogic _automationMeshLogic;

    /// <summary>
    /// Initializes a new instance of the <see cref="InternetConnectionMonitoringLogic"/> class.
    /// </summary>
    public InternetConnectionMonitoringLogic()
    {
        _automationMeshLogic = new AutomationMeshLogic();
    }

    /// <summary>
    /// Upserts one internet connection refresh into the local mesh and recomputes the aggregate connection status.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This helper creates the local mesh on demand when it does not exist yet, which makes it suitable for autonomous monitoring workers.
    /// </para>
    /// </remarks>
    public async Task<InternetConnectionMonitoringResult> RefreshLocalConnectionAsync(
        InternetConnectionStatusRefresh refresh,
        string machineName,
        string graphApiBaseUri,
        CancellationToken cancellationToken = default)
    {
        if (refresh == null || string.IsNullOrWhiteSpace(refresh.ConnectionId))
            return null;

        AutomationMesh mesh = await _automationMeshLogic.GetOrCreateLocalAsync(machineName, graphApiBaseUri, cancellationToken);
        InternetConnection connection = AutomationMeshLogic.UpsertInternetConnection(mesh, refresh, DateTimeOffset.UtcNow);
        bool meshStatusChanged = AutomationMeshLogic.RefreshInternetConnectionStatus(mesh);
        await _automationMeshLogic.SaveAsync(mesh, cancellationToken);

        return new InternetConnectionMonitoringResult()
        {
            Connection = connection,
            Mesh = mesh,
            MeshStatusChanged = meshStatusChanged
        };
    }
}