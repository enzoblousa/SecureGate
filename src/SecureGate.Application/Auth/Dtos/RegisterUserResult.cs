namespace SecureGate.Application.Auth.Dtos;

public sealed record RegisterUserResult(Guid Id, string Name, string Email, DateTime CreatedAt);
