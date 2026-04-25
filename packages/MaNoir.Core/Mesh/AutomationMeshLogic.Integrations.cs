using MaNoir.Core.Contracts.Models.Mesh;
using System;
using System.Linq;

namespace MaNoir.Core.Mesh;

public sealed partial class AutomationMeshLogic
{
    /// <summary>
    /// Applies source code integration settings to a mesh.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <param name="integration">The integration settings to apply.</param>
    public static void ApplySourceCodeIntegration(AutomationMesh mesh, AutomationMeshSouceCodeIntegration integration)
    {
        if (mesh == null)
            return;

        mesh.SourceCodeIntegration = integration;
    }

    /// <summary>
    /// Creates or updates an internet connection entry inside a mesh.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <param name="statusRefresh">The incoming status refresh.</param>
    /// <param name="now">The current business time.</param>
    /// <returns>The updated connection, or <see langword="null"/> when the input is incomplete.</returns>
    public static InternetConnection UpsertInternetConnection(AutomationMesh mesh, InternetConnectionStatusRefresh statusRefresh, DateTimeOffset now)
    {
        if (mesh == null || statusRefresh == null || string.IsNullOrWhiteSpace(statusRefresh.ConnectionId))
            return null;

        if (mesh.InternetConnections == null)
            mesh.InternetConnections = [];

        InternetConnection connection = mesh.InternetConnections.FirstOrDefault(item =>
            string.Equals(item.Id, statusRefresh.ConnectionId, StringComparison.InvariantCultureIgnoreCase));

        if (connection == null)
        {
            connection = new InternetConnection()
            {
                Id = statusRefresh.ConnectionId,
                ConnectionType = statusRefresh.ConnectionType,
                IsMain = mesh.InternetConnections.Count == 0
            };

            mesh.InternetConnections.Add(connection);
        }

        connection.ConnectionType = statusRefresh.ConnectionType;
        connection.DownloadBandwith = statusRefresh.DownloadBandwith;
        connection.UsedDownloadBandwith = statusRefresh.UsedDownloadBandwith;
        connection.UploadBandwith = statusRefresh.UploadBandwith;
        connection.UsedUploadBandwith = statusRefresh.UsedUploadBandwith;
        connection.LastMessage = statusRefresh.Message;
        connection.LastUpdate = now;
        connection.Status = statusRefresh.Status;

        if (statusRefresh.Ssids != null && statusRefresh.Ssids.Length > 0)
            mesh.MainSsid = statusRefresh.Ssids.First();

        return connection;
    }

    /// <summary>
    /// Computes the aggregate internet status code for a mesh.
    /// </summary>
    /// <param name="mesh">The mesh whose connection status should be evaluated.</param>
    /// <returns>The aggregate mesh internet status code.</returns>
    public static string ComputeInternetConnectionStatusCode(AutomationMesh mesh)
    {
        if (mesh == null || mesh.InternetConnections == null || mesh.InternetConnections.Count == 0)
            return AutomationMeshStatus.StatusOK;

        int countOk = mesh.InternetConnections.Count(connection => connection.Status == ConnectionStatus.Up);
        int countNotOk = mesh.InternetConnections.Count - countOk;

        if (countNotOk == mesh.InternetConnections.Count)
            return AutomationMeshStatus.StatusKO;

        if (countOk == mesh.InternetConnections.Count)
            return AutomationMeshStatus.StatusOK;

        return AutomationMeshStatus.StatusPartiallyOK;
    }

    /// <summary>
    /// Updates the aggregate mesh internet status code from the current connections.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <returns><see langword="true"/> when the status code changed.</returns>
    public static bool RefreshInternetConnectionStatus(AutomationMesh mesh)
    {
        if (mesh == null)
            return false;

        if (mesh.Status == null)
            mesh.Status = new AutomationMeshStatus();

        string newStatusCode = ComputeInternetConnectionStatusCode(mesh);
        if (string.Equals(mesh.Status.InternetConnectionStatusCode, newStatusCode, StringComparison.InvariantCulture))
            return false;

        mesh.Status.InternetConnectionStatusCode = newStatusCode;
        return true;
    }

    // TODO: Migrate mesh extension lifecycle rules.
    // Legacy surface:
    // - GET/POST/DELETE local/extensions
    // - GET local/extensions/{extensionId}/restart
    // - GET local/extensions/{extensionId}/install
    // - GET local/extensions/{extensionId}/uninstall
    // - GET local/extensions/{extensionId}/setinstalled

    // TODO: Migrate integration configuration rules.
    // Legacy surface:
    // - GET local/integrations
    // - GET local/integrations/byagent/{agentId}
    // - POST local/integrations
    // - POST local/integrations/raw
    // - GET/POST local/integrations/{integrationId}/config/{instanceId?}

    // TODO: Migrate location information enrichment rules.
    // Legacy surface:
    // - POST local/location/infos/weatherhazard
    // - POST local/location/infos/weather
}