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
        ErzaRuntime runtime = new ErzaRuntime();
        ErzaMessageRouter messageRouter = new ErzaMessageRouter(runtime);

        builder.Services.AddSingleton<IHostedService>(_ => new LifecycleHeartbeatService(runtime));
        builder.Services.AddSingleton<IHostedService>(_ => new MessagePumpService(runtime, messageRouter));
        builder.Services.AddSingleton<IHostedService>(_ => new PresenceMaintenanceService(runtime));
        builder.Services.AddSingleton<IHostedService>(_ => new NetworkConnectivityService(runtime));

        using IHost host = builder.Build();
        await host.RunAsync();
    }
}