using System;
using System.Threading.Tasks;
using MaNoir.Core.Mesh;
using MaNoir.Core.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MaNoir.Agents.Erza;

public static class Program
{
    public static async Task Main(string[] args)
    {
        await WaitForLocalMeshAsync();

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        builder.AddMaNoirAgentObservability("manoir-agent-erza");
        builder.Services.AddSingleton<ErzaRuntime>();
        builder.Services.AddSingleton<ErzaMessageRouter>();
        builder.Services.AddSingleton<IHostedService, LifecycleHeartbeatService>();
        builder.Services.AddSingleton<IHostedService, MessagePumpService>();
        builder.Services.AddSingleton<IHostedService, PresenceMaintenanceService>();
        builder.Services.AddSingleton<IHostedService, NetworkConnectivityService>();

        using IHost host = builder.Build();
        await host.RunAsync();
    }

    private static async Task WaitForLocalMeshAsync()
    {
        AutomationMeshLogic meshLogic = new AutomationMeshLogic();

        while (true)
        {
            try
            {
                if (await meshLogic.GetLocalAsync() != null)
                    return;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Could not check the local mesh: {exception.Message}");
            }

            Console.WriteLine("Waiting for the local mesh before starting Erza services.");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}