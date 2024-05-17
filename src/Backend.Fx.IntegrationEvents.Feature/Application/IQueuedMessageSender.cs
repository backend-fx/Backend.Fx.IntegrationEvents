namespace Backend.Fx.IntegrationEvents.Feature.Application;

public interface IQueuedMessageSender
{
    Task SendQueuedMessagesAsync(CancellationToken cancellationToken);
}
