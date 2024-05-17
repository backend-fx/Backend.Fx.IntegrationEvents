using System.Reflection;
using Backend.Fx.Execution;
using Backend.Fx.Execution.DependencyInjection;
using Backend.Fx.Execution.Pipeline;
using Backend.Fx.IntegrationEvents.Feature.Application;
using Backend.Fx.IntegrationEvents.Feature.MessageBus;
using Backend.Fx.Logging;
using Backend.Fx.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Backend.Fx.IntegrationEvents.Feature;

public class IntegrationEventsModule : IModule
{
    private readonly ILogger _logger = Log.Create<IntegrationEventsModule>();
    private readonly IBackendFxApplication _application;
    private readonly IMessageBus _messageBus;
    private readonly Assembly[] _assemblies;
    private readonly IIntegrationEventMessageSerializer _serializer;

    public IntegrationEventsModule(
        IBackendFxApplication application,
        IMessageBus messageBus,
        Assembly[] assemblies,
        IIntegrationEventMessageSerializer serializer)
    {
        _application = application;
        _messageBus = messageBus;
        _assemblies = assemblies;
        _serializer = serializer;
    }

    public void Register(ICompositionRoot compositionRoot)
    {
        // singleton message bus
        compositionRoot.Register(ServiceDescriptor.Singleton(_messageBus));

        // message serializer
        compositionRoot.Register(ServiceDescriptor.Singleton(_serializer));

        RegisterSendingServices(compositionRoot);
        RegisterReceivingServices(compositionRoot);
    }

    private void RegisterSendingServices(ICompositionRoot compositionRoot)
    {
        // the integration event scope records all integration events, serializes them and sends them on the message bus
        compositionRoot.Register(ServiceDescriptor.Scoped(
            sp => new IntegrationEventScope(
                sp.GetRequiredService<IClock>(),
                sp.GetRequiredService<ICurrentTHolder<Correlation>>(),
                _messageBus,
                sp.GetRequiredService<IIntegrationEventMessageSerializer>())));

        // it is visible to the application ony as integration event publisher, to enqueue integration events...
        compositionRoot.Register(
            ServiceDescriptor.Scoped<IIntegrationEventPublisher>(
                sp => sp.GetRequiredService<IntegrationEventScope>()));

        // but for the framework it provides an additional method to actually send those integration
        // event as messages on the event bus
        compositionRoot.Register(
            ServiceDescriptor.Scoped<IQueuedMessageSender>(
                sp => sp.GetRequiredService<IntegrationEventScope>()));

        // the operation in decorated, so that after completion of the operation, all enqueued messages are sent
        compositionRoot.RegisterDecorator(ServiceDescriptor
            .Scoped<IOperation, SendIntegrationEventMessagesAfterCompletionDecorator>());
    }

    private void RegisterReceivingServices(ICompositionRoot compositionRoot)
    {
        // find all integration event types
        foreach (Type integrationEventType in _assemblies.GetImplementingTypes(typeof(IIntegrationEvent)))
        {
            // each event can have multiple handlers
            HandlingTuple[] handlingTuples = GetHandlingTuples(integrationEventType);

            if (handlingTuples.Any())
            {
                foreach (var handlingTuple in handlingTuples)
                {
                    WireUpHandler(compositionRoot, handlingTuple);
                }
            }
            else
            {
                _logger.LogWarning("No handlers for {IntegrationEventType} found", integrationEventType);
            }
        }
    }

    private void WireUpHandler(ICompositionRoot compositionRoot, HandlingTuple handlingTuple)
    {
        _logger.LogInformation(
            "Registering handler {HandlerType} for {IntegrationEventType}",
            handlingTuple.ConcreteHandlerType,
            handlingTuple.IntegrationEventType);

        // the handler itself is registered as scoped service
        compositionRoot.Register(handlingTuple.GetScopedServiceDescriptor());

        // subscribe for the respective message type
        _messageBus.RegisterHandler(
            _serializer.GetMessageKey(handlingTuple.IntegrationEventType),
            async (message, cancellationToken) =>
            {
                MessageBusMessage messageBusMessage = _serializer.Deserialize(message);

                _logger.LogInformation(
                    "Received integration event {IntegrationEvent}",
                    messageBusMessage.Payload.GetType().Name);

                await _application.Invoker.InvokeAsync(async (sp, ct) =>
                {
                    var correlationHolder = sp.GetRequiredService<ICurrentTHolder<Correlation>>();
                    correlationHolder.Current.Resume(messageBusMessage.CorrelationId);

                    _logger.LogInformation("Calling handler {HandlerType}", handlingTuple.ConcreteHandlerType);
                    var handler = sp.GetRequiredService(handlingTuple.ConcreteHandlerType);
                    var methodInfo = handlingTuple.ConcreteHandlerType.GetMethod(
                        "HandleAsync",
                        BindingFlags.Instance | BindingFlags.Public);

                    Task? task = methodInfo?.Invoke(handler, [messageBusMessage.Payload, ct]) as Task;
                    await (task ?? Task.CompletedTask);
                }, new SystemIdentity(), cancellationToken);
            });
    }

    private HandlingTuple[] GetHandlingTuples(Type integrationEventType)
    {
        Type openGenericHandlerTypeForThisIntegrationEventType =
            typeof(IIntegrationEventHandler<>).MakeGenericType(integrationEventType);

        var handlingTuples = _assemblies
            .GetImplementingTypes(openGenericHandlerTypeForThisIntegrationEventType)
            .Select(t => new HandlingTuple(integrationEventType, t))
            .ToArray();

        return handlingTuples;
    }

    private record HandlingTuple(Type IntegrationEventType, Type ConcreteHandlerType)
    {
        public ServiceDescriptor GetScopedServiceDescriptor()
        {
            return new ServiceDescriptor(ConcreteHandlerType, ConcreteHandlerType, ServiceLifetime.Scoped);
        }
    }
}
