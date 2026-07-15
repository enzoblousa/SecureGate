using Microsoft.Extensions.Logging.Abstractions;
using SecureGate.Application.Auth;
using SecureGate.Application.Auth.Dtos;
using SecureGate.Application.Events;
using SecureGate.Domain.Entities;
using Xunit;

namespace SecureGate.Tests.Auth;

public class RegisterUserServiceTests
{
    private static RegisterUserService CreateService(
        out FakeUserRepository repository,
        out FakePasswordHasher hasher,
        out FakeEventPublisher eventPublisher)
    {
        repository = new FakeUserRepository();
        hasher = new FakePasswordHasher();
        eventPublisher = new FakeEventPublisher();
        return new RegisterUserService(repository, hasher, eventPublisher, NullLogger<RegisterUserService>.Instance);
    }

    private static RegisterUserService CreateService(out FakeUserRepository repository, out FakePasswordHasher hasher) =>
        CreateService(out repository, out hasher, out _);

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

    [Fact]
    public async Task RegisterAsync_WithValidData_PublishesUserRegisteredEvent()
    {
        var service = CreateService(out _, out _, out var eventPublisher);
        var request = new RegisterUserRequest("Ana Souza", "ana@example.com", "senha1234");

        var result = await service.RegisterAsync(request);

        var publishedEvent = Assert.Single(eventPublisher.PublishedEvents);
        var userRegisteredEvent = Assert.IsType<UserRegisteredEvent>(publishedEvent);
        Assert.Equal(result.Id, userRegisteredEvent.UserId);
        Assert.Equal("Ana Souza", userRegisteredEvent.Name);
        Assert.Equal("ana@example.com", userRegisteredEvent.Email);
    }

    [Fact]
    public async Task RegisterAsync_WhenEventPublishingFails_StillPersistsUserAndReturnsResult()
    {
        var service = CreateService(out var repository, out _, out var eventPublisher);
        eventPublisher.ExceptionToThrow = new InvalidOperationException("RabbitMQ indisponível");
        var request = new RegisterUserRequest("Ana Souza", "ana@example.com", "senha1234");

        var result = await service.RegisterAsync(request);

        Assert.Equal("Ana Souza", result.Name);
        Assert.Single(repository.Users);
    }
}
