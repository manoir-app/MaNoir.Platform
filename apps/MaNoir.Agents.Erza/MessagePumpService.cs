using System;
using System.Threading;
using System.Threading.Tasks;
using Home.Common;
using Microsoft.Extensions.Hosting;

namespace MaNoir.Agents.Erza;

public sealed class MessagePumpService : BackgroundService
{
    private readonly ErzaMessageRouter _messageRouter;
    private readonly ErzaRuntime _runtime;

    public MessagePumpService(ErzaRuntime runtime, ErzaMessageRouter messageRouter)
    {
        _messageRouter = messageRouter;
        _runtime = runtime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _runtime.ReportTopicsSubscribed();

        Task listenerTask = Task.Run(() => NatsInterprocessListener.Run(_runtime.MessageTopics, _messageRouter.HandleMessage), CancellationToken.None);
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
            _runtime.ReportInterprocessStopped();
            await listenerTask;
        }
    }
}