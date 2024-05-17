using System.Collections.Concurrent;
using Backend.Fx.Execution.Pipeline;
using Backend.Fx.IntegrationEvents.Feature.MessageBus;
using Backend.Fx.Util;
using NodaTime;

namespace Backend.Fx.IntegrationEvents.Feature.Application;

/// <summary>
/// The integration event scope keeps track of all integration events that are published during an operation. Messages
/// are not sent earlier than the method SendQueuedMessagesAsync is called. This is done by a decorator when the
/// operation completed. This way we ensure that all event messages are sent when they are safely persisted and a
/// possible transaction was committed.
/// </summary>
public class IntegrationEventScope(
    IClock clock,
    ICurrentTHolder<Correlation> correlationHolder,
    IMessageBus messageBus,
    IIntegrationEventMessageSerializer serializer)
    : IIntegrationEventPublisher, IQueuedMessageSender
{
    private readonly ConcurrentQueue<SerializedMessage> _queuedMessages = new();
    private bool _canPublish = true;

    public void Publish(IIntegrationEvent integrationEvent)
    {
        if (!_canPublish)
        {
            throw new InvalidOperationException(
                "This integration event scope is closed, because sending of messages has started. No more events can be published.");
        }

        var message = new MessageBusMessage
        {
            Id = Guid.NewGuid(),
            CreationDate = clock.GetCurrentInstant(),
            CorrelationId = correlationHolder.Current.Id,
            Payload = integrationEvent
        };

        SerializedMessage serializedMessage = serializer.Serialize(message);
        _queuedMessages.Enqueue(serializedMessage);
    }

    public async Task SendQueuedMessagesAsync(CancellationToken cancellationToken)
    {
        _canPublish = false;

        while (_queuedMessages.TryDequeue(out var queuedMessage))
        {
            await messageBus.PublishAsync(queuedMessage, cancellationToken);
        }
    }
}
