using System.Collections.Concurrent;
using System.Text.Json;
using NodaTime;

namespace Backend.Fx.IntegrationEvents.Feature.MessageBus;

public class JsonIntegrationEventMessageSerializer : IIntegrationEventMessageSerializer
{
    private readonly ConcurrentDictionary<string, Type> _typeCache = new();

    public SerializedMessage Serialize(MessageBusMessage message)
    {
        try
        {
            var messagePayload = JsonSerializer.SerializeToUtf8Bytes(message.Payload);
            var integrationEventType = message.Payload.GetType();
            var messageType = GetMessageKey(integrationEventType);
            _typeCache.AddOrUpdate(messageType, s => integrationEventType, (s, t) => integrationEventType);
            return new SerializedMessage(messageType, messagePayload);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error serializing message", ex);
        }
    }

    public MessageBusMessage Deserialize(SerializedMessage serializedMessage)
    {
        try
        {
            var payloadType = _typeCache.GetOrAdd(
                serializedMessage.MessageType,
                s => AppDomain.CurrentDomain.GetAssemblies()
                         .SelectMany(ass => ass.GetTypes())
                         .SingleOrDefault(t => t.FullName == s)
                     ?? throw new NotSupportedException($"Cannot find type: {serializedMessage.MessageType}"));

            var payload = JsonSerializer.Deserialize(serializedMessage.MessagePayload, payloadType);
            if (payload == null)
            {
                throw new InvalidDataException("The payload of the message was null");
            }

            var message = new MessageBusMessage
            {
                Id = Guid.NewGuid(),
                CreationDate = SystemClock.Instance.GetCurrentInstant(),
                CorrelationId = Guid.NewGuid(),
                Payload = payload
            };

            return message;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error deserializing message", ex);
        }
    }

    public string GetMessageKey(Type integrationEventType)
    {
        var messageKey = integrationEventType.FullName ??
                         throw new InvalidDataException("the type of the integration event has no name");


        return messageKey;
    }
}
