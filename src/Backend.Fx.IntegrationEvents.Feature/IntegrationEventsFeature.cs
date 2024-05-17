using Backend.Fx.Execution;
using Backend.Fx.Execution.Features;
using Backend.Fx.IntegrationEvents.Feature.MessageBus;
using Backend.Fx.Logging;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Backend.Fx.IntegrationEvents.Feature;

/// <summary>
/// The feature "Integration Events" provides you with infrastructure to send and handle incoming integration events.
/// An integration event is defined as something that already happened, but should be handled in a separate context.
/// Hence, these events are not sent during normal domain logic processing, but are sent after the operation is completed.
/// The integration events are serialized and sent to a message bus, where they are picked up by the receiving side.
/// Handling of incoming events is done in a separate scope, so that processing takes place in another transaction as the
/// operation that caused them.
/// </summary>
[PublicAPI]
public class IntegrationEventsFeature : Execution.Features.Feature, IBootableFeature
{
    private readonly ILogger _logger = Log.Create<IntegrationEventsFeature>();
    private IMessageBus _messageBus;
    private IIntegrationEventMessageSerializer _serializer;

    /// <summary>
    /// The feature "Integration Events" provides you with infrastructure to send and handle incoming integration events.
    /// An integration event is defined as something that already happened, but should be handled in a separate context.
    /// Hence, these events are not sent during normal domain logic processing, but are sent after the operation is completed.
    /// The integration events are serialized and sent to a message bus, where they are picked up by the receiving side.
    /// Handling of incoming events is done in a separate scope, so that processing takes place in another transaction as the
    /// operation that caused them.
    /// </summary>
    public IntegrationEventsFeature(
        IMessageBus? messageBus = null,
        IIntegrationEventMessageSerializer? serializer = null)
    {
        _messageBus = messageBus ?? new InProcMessageBus();
        _serializer = serializer ?? new JsonIntegrationEventMessageSerializer();
    }

    public override void Enable(IBackendFxApplication application)
    {
        application.CompositionRoot.RegisterModules(
            new IntegrationEventsModule(application, _messageBus, application.Assemblies, _serializer));
    }

    public async Task BootAsync(IBackendFxApplication application, CancellationToken cancellationToken = default)
    {
        using (_logger.LogInformationDuration("Connecting message bus...", "done"))
        {
            await _messageBus.ConnectAsync(cancellationToken);
        }

        using (_logger.LogInformationDuration("Subscribing to integration event messages...", "done"))
        {
            await _messageBus.SubscribeAsync(cancellationToken);
        }
    }
}
