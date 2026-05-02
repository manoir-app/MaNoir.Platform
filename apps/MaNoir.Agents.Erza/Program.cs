using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MaNoir.Agents.Erza;

public static class Program
{
    public static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddHttpClient<NetworkConnectivityService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        builder.Services.AddSingleton<ErzaRuntime>();
        builder.Services.AddSingleton<ErzaMessageRouter>();
        builder.Services.AddHostedService<LifecycleHeartbeatService>();
        builder.Services.AddHostedService<MessagePumpService>();
        builder.Services.AddHostedService<PresenceMaintenanceService>();
        builder.Services.AddHostedService<NetworkConnectivityService>();

        using IHost host = builder.Build();
        await host.RunAsync();
    }
}