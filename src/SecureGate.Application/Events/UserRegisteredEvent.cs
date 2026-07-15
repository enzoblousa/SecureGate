namespace SecureGate.Application.Events;

public sealed record UserRegisteredEvent(Guid UserId, string Name, string Email, DateTime RegisteredAt);
