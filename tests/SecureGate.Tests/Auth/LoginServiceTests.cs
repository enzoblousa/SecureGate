using SecureGate.Application.Auth;
using SecureGate.Application.Auth.Dtos;
using SecureGate.Domain.Entities;
using Xunit;

namespace SecureGate.Tests.Auth;

public class LoginServiceTests
{
    private static LoginService CreateService(
        out FakeUserRepository repository,
        out FakePasswordHasher hasher,
        out FakeTokenGenerator tokenGenerator)
    {
        repository = new FakeUserRepository();
        hasher = new FakePasswordHasher();
        tokenGenerator = new FakeTokenGenerator();
        return new LoginService(repository, hasher, tokenGenerator);
    }

    [Fact]
    public async Task LoginAsync_WithCorrectCredentials_ReturnsTokenForTheUser()
    {
        var service = CreateService(out var repository, out var hasher, out var tokenGenerator);
        await repository.AddAsync(new User("Ana Souza", "ana@example.com", hasher.Hash("senha1234")));

        var result = await service.LoginAsync(new LoginRequest("ana@example.com", "senha1234"));

        Assert.Equal("token-for:ana@example.com", result.Token);
        Assert.Equal("ana@example.com", tokenGenerator.LastUser!.Email);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ThrowsInvalidCredentialsException()
    {
        var service = CreateService(out _, out _, out _);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.LoginAsync(new LoginRequest("nobody@example.com", "senha1234")));
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsInvalidCredentialsException()
    {
        var service = CreateService(out var repository, out var hasher, out _);
        await repository.AddAsync(new User("Ana Souza", "ana@example.com", hasher.Hash("senha1234")));

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.LoginAsync(new LoginRequest("ana@example.com", "senhaerrada")));
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmailAndWrongPassword_ThrowTheSameExceptionMessage()
    {
        var service = CreateService(out var repository, out var hasher, out _);
        await repository.AddAsync(new User("Ana Souza", "ana@example.com", hasher.Hash("senha1234")));

        var notFoundException = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.LoginAsync(new LoginRequest("nobody@example.com", "senha1234")));
        var wrongPasswordException = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.LoginAsync(new LoginRequest("ana@example.com", "senhaerrada")));

        Assert.Equal(notFoundException.Message, wrongPasswordException.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    public async Task LoginAsync_WithInvalidEmail_ThrowsArgumentException(string? invalidEmail)
    {
        var service = CreateService(out _, out _, out _);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.LoginAsync(new LoginRequest(invalidEmail!, "senha1234")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LoginAsync_WithMissingPassword_ThrowsArgumentException(string? invalidPassword)
    {
        var service = CreateService(out _, out _, out _);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.LoginAsync(new LoginRequest("ana@example.com", invalidPassword!)));
    }
}
