namespace SecureGate.Application.Abstractions;

public interface IEmailSender
{
    Task SendWelcomeEmailAsync(string toEmail, string name, CancellationToken cancellationToken = default);
}
