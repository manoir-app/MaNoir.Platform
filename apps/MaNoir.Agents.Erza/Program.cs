using System;
using System.Threading.Tasks;
using MaNoir.Core.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MaNoir.Agents.Erza;

public static class Program
{
    public static async Task Main(string[] args)
    {
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
}