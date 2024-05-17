namespace Backend.Fx.IntegrationEvents.Feature.MessageBus;

public interface IIntegrationEventMessageSerializer
{
    SerializedMessage Serialize(MessageBusMessage message);

    MessageBusMessage Deserialize(SerializedMessage serializedMessage);

    string GetMessageKey(Type integrationEventType);
}
