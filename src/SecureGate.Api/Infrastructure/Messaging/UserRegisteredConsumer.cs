using MassTransit;
using SecureGate.Application.Events;

namespace SecureGate.Api.Infrastructure.Messaging;

public sealed class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(ILogger<UserRegisteredConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        _logger.LogInformation("E-mail de boas-vindas enviado para {Email}", context.Message.Email);
        return Task.CompletedTask;
    }
}
