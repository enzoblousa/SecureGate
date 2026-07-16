using MassTransit;
using SecureGate.Application.Abstractions;
using SecureGate.Application.Events;

namespace SecureGate.Api.Infrastructure.Messaging;

public sealed class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(IEmailSender emailSender, ILogger<UserRegisteredConsumer> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        try
        {
            await _emailSender.SendWelcomeEmailAsync(
                context.Message.Email,
                context.Message.Name,
                context.CancellationToken);

            _logger.LogInformation("E-mail de boas-vindas enviado para {Email}", context.Message.Email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar e-mail de boas-vindas para {Email}", context.Message.Email);
        }
    }
}
