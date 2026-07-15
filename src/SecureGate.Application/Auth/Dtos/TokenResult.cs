namespace SecureGate.Application.Auth.Dtos;

public sealed record TokenResult(string Token, DateTime ExpiresAt);
