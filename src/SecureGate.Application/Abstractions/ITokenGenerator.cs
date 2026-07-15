using SecureGate.Application.Auth.Dtos;
using SecureGate.Domain.Entities;

namespace SecureGate.Application.Abstractions;

public interface ITokenGenerator
{
    TokenResult Generate(User user);
}
