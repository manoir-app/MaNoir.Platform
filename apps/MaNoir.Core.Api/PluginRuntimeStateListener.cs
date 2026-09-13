using System;
using System.Threading;
using System.Threading.Tasks;
using Home.Common;
using Home.Common.Messages;
using MaNoir.Core.Contributions;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace MaNoir.Core.Api;

public sealed class PluginRuntimeStateListener : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Task listenerTask = Task.Run(() => NatsInterprocessListener.Run(
            [PluginRuntimeStateMessage.PublishTopic],
            HandleMessage), CancellationToken.None);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            NatsInterprocessListener.Stop();
            await listenerTask;
        }
    }

    private static MessageResponse HandleMessage(MessageOrigin origin, string topic, string messageBody)
    {
        PluginRuntimeStateMessage message = JsonConvert.DeserializeObject<PluginRuntimeStateMessage>(messageBody);
        if (message == null || string.IsNullOrWhiteSpace(message.PluginId))
            return MessageResponse.GenericFail;

        try
        {
            new ContributionMongoOperations().UpdatePluginRuntimeStateAsync(
                message.PluginId,
                message.Components,
                CancellationToken.None).GetAwaiter().GetResult();
            return MessageResponse.OK;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return MessageResponse.GenericFail;
        }
    }
}