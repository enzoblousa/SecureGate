using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SecureGate.Api.Infrastructure.Messaging;
using SecureGate.Application.Abstractions;
using SecureGate.Application.Events;
using Xunit;

namespace SecureGate.Tests.Messaging;

public class UserRegisteredConsumerTests
{
    [Fact]
    public async Task Consume_SendsWelcomeEmailWithTheEventEmailAndName()
    {
        var fakeLogger = new FakeLogger<UserRegisteredConsumer>();
        var fakeEmailSender = new FakeEmailSender();

        var services = new ServiceCollection();
        services.AddSingleton<ILogger<UserRegisteredConsumer>>(fakeLogger);
        services.AddSingleton<IEmailSender>(fakeEmailSender);
        services.AddMassTransitTestHarness(x => x.AddConsumer<UserRegisteredConsumer>());

        await using var provider = services.BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new UserRegisteredEvent(Guid.NewGuid(), "Ana Souza", "ana@example.com", DateTime.UtcNow);
        await harness.Bus.Publish(@event);

        Assert.True(await harness.Consumed.Any<UserRegisteredEvent>());
        var sentEmail = Assert.Single(fakeEmailSender.SentEmails);
        Assert.Equal("ana@example.com", sentEmail.ToEmail);
        Assert.Equal("Ana Souza", sentEmail.Name);
    }

    [Fact]
    public async Task Consume_LogsSuccessMessageWithTheEventEmail()
    {
        var fakeLogger = new FakeLogger<UserRegisteredConsumer>();
        var fakeEmailSender = new FakeEmailSender();

        var services = new ServiceCollection();
        services.AddSingleton<ILogger<UserRegisteredConsumer>>(fakeLogger);
        services.AddSingleton<IEmailSender>(fakeEmailSender);
        services.AddMassTransitTestHarness(x => x.AddConsumer<UserRegisteredConsumer>());

        await using var provider = services.BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new UserRegisteredEvent(Guid.NewGuid(), "Ana Souza", "ana@example.com", DateTime.UtcNow);
        await harness.Bus.Publish(@event);

        Assert.True(await harness.Consumed.Any<UserRegisteredEvent>());
        Assert.Contains(fakeLogger.Messages, message => message.Contains("ana@example.com"));
    }

    [Fact]
    public async Task Consume_WhenEmailSendingFails_StillProcessesTheMessageWithoutThrowing()
    {
        var fakeLogger = new FakeLogger<UserRegisteredConsumer>();
        var fakeEmailSender = new FakeEmailSender { ExceptionToThrow = new InvalidOperationException("smtp4dev indisponível") };

        var services = new ServiceCollection();
        services.AddSingleton<ILogger<UserRegisteredConsumer>>(fakeLogger);
        services.AddSingleton<IEmailSender>(fakeEmailSender);
        services.AddMassTransitTestHarness(x => x.AddConsumer<UserRegisteredConsumer>());

        await using var provider = services.BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new UserRegisteredEvent(Guid.NewGuid(), "Ana Souza", "ana@example.com", DateTime.UtcNow);
        await harness.Bus.Publish(@event);

        Assert.True(await harness.Consumed.Any<UserRegisteredEvent>());
        Assert.Empty(fakeEmailSender.SentEmails);
    }
}
