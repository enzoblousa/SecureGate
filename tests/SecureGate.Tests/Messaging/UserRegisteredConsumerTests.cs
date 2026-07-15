using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SecureGate.Api.Infrastructure.Messaging;
using SecureGate.Application.Events;
using Xunit;

namespace SecureGate.Tests.Messaging;

public class UserRegisteredConsumerTests
{
    [Fact]
    public async Task Consume_LogsWelcomeEmailMessageWithTheEventEmail()
    {
        var fakeLogger = new FakeLogger<UserRegisteredConsumer>();

        var services = new ServiceCollection();
        services.AddSingleton<ILogger<UserRegisteredConsumer>>(fakeLogger);
        services.AddMassTransitTestHarness(x => x.AddConsumer<UserRegisteredConsumer>());

        await using var provider = services.BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new UserRegisteredEvent(Guid.NewGuid(), "Ana Souza", "ana@example.com", DateTime.UtcNow);
        await harness.Bus.Publish(@event);

        Assert.True(await harness.Consumed.Any<UserRegisteredEvent>());
        Assert.Contains(fakeLogger.Messages, message => message.Contains("ana@example.com"));
    }
}
