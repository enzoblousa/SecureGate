using Microsoft.EntityFrameworkCore;
using SecureGate.Api.Infrastructure.Persistence;
using SecureGate.Domain.Entities;
using Testcontainers.PostgreSql;
using Xunit;

namespace SecureGate.Tests.Persistence;

public sealed class UserRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    private AppDbContext _dbContext = null!;
    private UserRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _dbContext = new AppDbContext(CreateOptions());
        await _dbContext.Database.EnsureCreatedAsync();

        _repository = new UserRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private DbContextOptions<AppDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

    [Fact]
    public async Task AddAsync_PersistsUserInThePostgresDatabase()
    {
        var user = new User("Ana Souza", "ana@example.com", "hashed-password");

        await _repository.AddAsync(user);

        await using var freshContext = new AppDbContext(CreateOptions());
        var persisted = await freshContext.Users.SingleAsync(u => u.Id == user.Id);

        Assert.Equal("Ana Souza", persisted.Name);
        Assert.Equal("ana@example.com", persisted.Email);
        Assert.Equal("hashed-password", persisted.PasswordHash);
    }

    [Fact]
    public async Task GetByEmailAsync_WithExistingEmail_ReturnsTheUser()
    {
        var user = new User("Ana Souza", "ana@example.com", "hashed-password");
        await _repository.AddAsync(user);

        var found = await _repository.GetByEmailAsync("ana@example.com");

        Assert.NotNull(found);
        Assert.Equal(user.Id, found!.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_WithNoMatchingEmail_ReturnsNull()
    {
        var found = await _repository.GetByEmailAsync("nobody@example.com");

        Assert.Null(found);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateEmail_ThrowsBecauseOfTheUniqueIndex()
    {
        await _repository.AddAsync(new User("Ana Souza", "ana@example.com", "hashed-password"));

        var duplicate = new User("Outra Pessoa", "ana@example.com", "another-hash");

        await Assert.ThrowsAsync<DbUpdateException>(() => _repository.AddAsync(duplicate));
    }
}
