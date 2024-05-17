using JetBrains.Annotations;

namespace Backend.Fx.IntegrationEvents;

[PublicAPI]
public interface IIntegrationEventPublisher
{
    void Publish(IIntegrationEvent integrationEvent);
}
