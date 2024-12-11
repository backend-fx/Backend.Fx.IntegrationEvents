using System.Threading;
using System.Threading.Tasks;
using Backend.Fx.Execution;
using Backend.Fx.Execution.SimpleInjector;
using Backend.Fx.Logging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Fx.IntegrationEvents.Feature.Tests;

public class TheIntegrationEventFeature
{
    [Fact]
    public async Task CallsTheEventHandler()
    {
        var app = new TestApplication();
        app.EnableFeature(new IntegrationEventsFeature());
        await app.BootAsync();

        Assert.Equal(0, Handler.LastNumber);
        
        await app.Invoker.InvokeAsync((sp, _) =>
        {
            var publisher = sp.GetRequiredService<IIntegrationEventPublisher>();
            publisher.Publish(new AnEvent(42));
            return Task.CompletedTask;
        });

        // handling is async, so we wait a bit
        await Task.Delay(100);

        Assert.Equal(42, Handler.LastNumber);
    }

    private class TestApplication()
        : BackendFxApplication(
            new SimpleInjectorCompositionRoot(),
            new DebugExceptionLogger(),
            typeof(TheIntegrationEventFeature).Assembly);
}

public class AnEvent : IIntegrationEvent
{
    public AnEvent(int number)
    {
        Number = number;
    }

    public int Number { get; set; }
}

public class Handler : IIntegrationEventHandler<AnEvent>
{
    public static int LastNumber { get; private set; }

    public Task HandleAsync(AnEvent integrationEvent, CancellationToken cancellationToken)
    {
        LastNumber = integrationEvent.Number;
        return Task.CompletedTask;
    }
}
