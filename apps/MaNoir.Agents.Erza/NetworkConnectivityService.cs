using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.Mesh;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Agents.Erza;

public sealed class NetworkConnectivityService : BackgroundService
{
    private readonly HttpClient _httpClient;
    private readonly InternetConnectionMonitoringLogic _monitoringLogic;
    private readonly ErzaRuntime _runtime;

    public NetworkConnectivityService(HttpClient httpClient, ErzaRuntime runtime)
    {
        _httpClient = httpClient;
        _monitoringLogic = new InternetConnectionMonitoringLogic();
        _runtime = runtime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(_runtime.NetworkConnectivityInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RefreshConnectivityAsync(stoppingToken);

            if (!await timer.WaitForNextTickAsync(stoppingToken))
                return;
        }
    }

    private async Task RefreshConnectivityAsync(CancellationToken cancellationToken)
    {
        string diagnosticMessage = "All probes failed.";
        ConnectionStatus connectionStatus = ConnectionStatus.Down;

        foreach (string probeUrl in _runtime.NetworkProbeUrls)
        {
            try
            {
                using HttpRequestMessage request = new(HttpMethod.Get, probeUrl);
                using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    connectionStatus = ConnectionStatus.Up;
                    diagnosticMessage = $"Probe OK: {probeUrl}";
                    break;
                }

                diagnosticMessage = $"Probe {(int)response.StatusCode} on {probeUrl}";
            }
            catch (Exception exception) when (exception is HttpRequestException || exception is TaskCanceledException)
            {
                diagnosticMessage = $"Probe failed on {probeUrl}: {exception.Message}";
            }
        }

        InternetConnectionMonitoringResult result = await _monitoringLogic.RefreshLocalConnectionAsync(
            new InternetConnectionStatusRefresh()
            {
                ConnectionId = _runtime.NetworkConnectionId,
                ConnectionType = _runtime.NetworkConnectionType,
                Status = connectionStatus,
                Message = diagnosticMessage
            },
            _runtime.MachineName,
            _runtime.GraphApiBaseUri,
            cancellationToken);

        _runtime.ReportNetworkConnectivityRefresh(result?.Connection, result?.MeshStatusChanged == true);
    }
}