using JetBrains.Annotations;
using NodaTime;

namespace Backend.Fx.IntegrationEvents.Feature.MessageBus;

[PublicAPI]
public class MessageBusMessage
{
    public required Guid Id { get; set; }

    public required Instant CreationDate { get; set; }

    public required Guid CorrelationId { get; set; }

    public required object Payload { get; set; }
}
