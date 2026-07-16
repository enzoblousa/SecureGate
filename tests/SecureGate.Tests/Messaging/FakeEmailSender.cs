using SecureGate.Application.Abstractions;

namespace SecureGate.Tests.Messaging;

public sealed class FakeEmailSender : IEmailSender
{
    private readonly List<(string ToEmail, string Name)> _sentEmails = new();

    public IReadOnlyList<(string ToEmail, string Name)> SentEmails => _sentEmails;

    public Exception? ExceptionToThrow { get; set; }

    public Task SendWelcomeEmailAsync(string toEmail, string name, CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        _sentEmails.Add((toEmail, name));
        return Task.CompletedTask;
    }
}
