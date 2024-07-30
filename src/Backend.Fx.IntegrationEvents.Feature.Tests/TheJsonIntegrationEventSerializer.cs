using System;
using Backend.Fx.IntegrationEvents;
using Backend.Fx.IntegrationEvents.Feature.MessageBus;
using NodaTime;
using Xunit;

namespace Backend.Fx.MessageBus.Feature.Tests;

public class TheJsonIntegrationEventSerializer
{
    private readonly JsonIntegrationEventMessageSerializer _sut = new();

    [Fact]
    public void CanSerializeAndDeserialize()
    {
        var theEvent = new SerializationTestEvent();
        var serialized = _sut.Serialize(new MessageBusMessage
        {
            CorrelationId = Guid.NewGuid(),
            CreationDate = SystemClock.Instance.GetCurrentInstant(),
            Id = Guid.NewGuid(),
            Payload = theEvent
        });

        var deserialized = _sut.Deserialize(serialized);

        // Assert.Equal(theEvent.Whatever, deserialized.Payload.Whatever);
        // Assert.Equal(theEvent.Number, deserialized.Payload.Number);
    }
}

public class SerializationTestEvent : IIntegrationEvent
{
    public string Whatever { get; set; } = "it takes";

    public int Number { get; set; } = 42;
}
