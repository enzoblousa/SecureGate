using SecureGate.Application.Auth;
using SecureGate.Application.Auth.Dtos;
using SecureGate.Domain.Entities;
using Xunit;

namespace SecureGate.Tests.Auth;

public class RegisterUserServiceTests
{
    private static RegisterUserService CreateService(out FakeUserRepository repository, out FakePasswordHasher hasher)
    {
        repository = new FakeUserRepository();
        hasher = new FakePasswordHasher();
        return new RegisterUserService(repository, hasher);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_PersistsUserAndReturnsResult()
    {
        var service = CreateService(out var repository, out _);
        var request = new RegisterUserRequest("Ana Souza", "ana@example.com", "senha1234");

        var result = await service.RegisterAsync(request);

        Assert.Equal("Ana Souza", result.Name);
        Assert.Equal("ana@example.com", result.Email);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Single(repository.Users);
        Assert.Equal("hashed:senha1234", repository.Users[0].PasswordHash);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsEmailAlreadyInUseException()
    {
        var service = CreateService(out var repository, out var hasher);
        await repository.AddAsync(new User("Ana Souza", "ana@example.com", hasher.Hash("senha1234")));

        var request = new RegisterUserRequest("Outra Pessoa", "ana@example.com", "outrasenha");

        await Assert.ThrowsAsync<EmailAlreadyInUseException>(() => service.RegisterAsync(request));
        Assert.Single(repository.Users);
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("")]
    [InlineData(null)]
    public async Task RegisterAsync_WithPasswordShorterThan8Characters_ThrowsArgumentException(string? invalidPassword)
    {
        var service = CreateService(out var repository, out _);
        var request = new RegisterUserRequest("Ana Souza", "ana@example.com", invalidPassword!);

        await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterAsync(request));
        Assert.Empty(repository.Users);
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidEmailFormat_ThrowsArgumentException()
    {
        var service = CreateService(out var repository, out _);
        var request = new RegisterUserRequest("Ana Souza", "not-an-email", "senha1234");

        await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterAsync(request));
        Assert.Empty(repository.Users);
    }
}
