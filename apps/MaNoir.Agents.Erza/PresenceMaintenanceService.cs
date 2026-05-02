using System;
using System.Threading;
using System.Threading.Tasks;
using MaNoir.Core.Users;
using Microsoft.Extensions.Hosting;

namespace MaNoir.Agents.Erza;

public sealed class PresenceMaintenanceService : BackgroundService
{
    private readonly PresenceLogic _presenceLogic;
    private readonly ErzaRuntime _runtime;

    public PresenceMaintenanceService(ErzaRuntime runtime)
    {
        _presenceLogic = new PresenceLogic();
        _runtime = runtime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(_runtime.PresenceMaintenanceInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            PresenceChangeSet changeSet = await _presenceLogic.RunMaintenanceAsync(stoppingToken);
            _runtime.PublishPresenceChanges(changeSet);
            _runtime.RunPresenceMaintenance();
        }
    }
}