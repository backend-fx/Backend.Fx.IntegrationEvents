namespace Backend.Fx.IntegrationEvents.Feature.MessageBus;

/// <summary>
/// A message bus implementation that runs in the same process. Messages are serialized and dispatched asynchronously
/// as in a real implementation.
/// </summary>
public class InProcMessageBus : IMessageBus
{
    private readonly TimeSpan _simulatedLatency;
    private readonly Dictionary<string, Func<SerializedMessage, CancellationToken, Task>> _handlers = new();

    public InProcMessageBus(TimeSpan simulatedLatency = default)
    {
        _simulatedLatency = simulatedLatency;
    }

    public void RegisterHandler(string messageType, Func<SerializedMessage, CancellationToken, Task> asyncHandler)
    {
        _handlers.Add(messageType, asyncHandler);
    }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SubscribeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task PublishAsync(SerializedMessage message, CancellationToken cancellationToken = default)
    {
        if (_handlers.TryGetValue(message.MessageType, out var handler))
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(_simulatedLatency, cancellationToken);
                await handler.Invoke(message, cancellationToken);
            }, cancellationToken);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}
