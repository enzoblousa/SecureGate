using SecureGate.Application.Abstractions;
using SecureGate.Application.Auth.Dtos;
using SecureGate.Domain.Entities;

namespace SecureGate.Tests.Auth;

public sealed class FakeTokenGenerator : ITokenGenerator
{
    public User? LastUser { get; private set; }

    public TokenResult Generate(User user)
    {
        LastUser = user;
        return new TokenResult($"token-for:{user.Email}", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }
}
