namespace Backend.Fx.IntegrationEvents.Feature.MessageBus;

public class SerializedMessage(string messageType, byte[] messagePayload)
{
    public string MessageType { get; } = messageType;
    public byte[] MessagePayload { get; } = messagePayload;
}
