namespace SecureGate.Application.Auth.Dtos;

public sealed record RegisterUserRequest(string Name, string Email, string Password);
