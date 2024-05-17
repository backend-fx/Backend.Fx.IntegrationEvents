using JetBrains.Annotations;

namespace Backend.Fx.IntegrationEvents.Feature.MessageBus;

[PublicAPI]
public interface IMessageBus : IDisposable
{
    /// <summary>
    /// Registers a handler that should become a subscription when calling SubscribeAsync
    /// </summary>
    /// <param name="messageType"></param>
    /// <param name="asyncHandler"></param>
    void RegisterHandler(string messageType, Func<SerializedMessage, CancellationToken, Task> asyncHandler);

    /// <summary>
    /// Connect to the message bus
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Make subscriptions for all registered handlers
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SubscribeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish a message to the message bus
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task PublishAsync(SerializedMessage message, CancellationToken cancellationToken = default);
}
