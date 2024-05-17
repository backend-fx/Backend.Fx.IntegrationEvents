using JetBrains.Annotations;
using NodaTime;

namespace Backend.Fx.IntegrationEvents.Feature.MessageBus;

[PublicAPI]
public class MessageBusMessage
{
    public Guid Id { get; set; }

    public Instant CreationDate { get; set; }

    public Guid CorrelationId { get; set; }

    public object Payload { get; set; }
}
