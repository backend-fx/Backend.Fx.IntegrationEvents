using JetBrains.Annotations;

namespace Backend.Fx.IntegrationEvents;

[PublicAPI]
public interface IIntegrationEventHandler<in TIntegrationEvent> where TIntegrationEvent : class, IIntegrationEvent
{
    Task HandleAsync(TIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
