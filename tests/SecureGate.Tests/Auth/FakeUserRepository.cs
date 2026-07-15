using SecureGate.Application.Abstractions;
using SecureGate.Domain.Entities;

namespace SecureGate.Tests.Auth;

public sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public IReadOnlyList<User> Users => _users;

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }
}
