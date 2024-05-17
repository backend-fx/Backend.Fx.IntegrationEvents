using Backend.Fx.Execution.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Fx.IntegrationEvents.Feature.Application;

/// <summary>
/// Makes sure that all enqueued integration events are published on the message bus when the operation completed.
/// </summary>
/// <param name="operation"></param>
/// <param name="queuedMessageSender"></param>
public class SendIntegrationEventMessagesAfterCompletionDecorator(
    IOperation operation,
    IQueuedMessageSender queuedMessageSender)
    : IOperation
{
    public Task BeginAsync(IServiceScope serviceScope, CancellationToken cancellationToken = default)
    {
        return operation.BeginAsync(serviceScope, cancellationToken);
    }

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        await operation.CompleteAsync(cancellationToken);
        await queuedMessageSender.SendQueuedMessagesAsync(cancellationToken);
    }

    public Task CancelAsync(CancellationToken cancellationToken = default)
    {
        return operation.CancelAsync(cancellationToken);
    }
}
