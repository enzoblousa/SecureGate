using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureGate.Api.Controllers;
using SecureGate.Api.Controllers.Contracts;
using SecureGate.Application.Auth;
using SecureGate.Application.Auth.Dtos;
using SecureGate.Domain.Entities;
using SecureGate.Tests.Auth;
using Xunit;

namespace SecureGate.Tests.Controllers;

public class AuthControllerTests
{
    private static AuthController CreateController(out FakeUserRepository repository)
    {
        repository = new FakeUserRepository();
        var service = new RegisterUserService(repository, new FakePasswordHasher());
        return new AuthController(service);
    }

    [Fact]
    public async Task Register_WithValidData_Returns201WithCreatedUser()
    {
        var controller = CreateController(out _);
        var request = new RegisterUserRequest("Ana Souza", "ana@example.com", "senha1234");

        var actionResult = await controller.Register(request, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        var body = Assert.IsType<RegisterUserResult>(objectResult.Value);
        Assert.Equal("Ana Souza", body.Name);
        Assert.Equal("ana@example.com", body.Email);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409WithErrorMessage()
    {
        var controller = CreateController(out var repository);
        await repository.AddAsync(new User("Ana Souza", "ana@example.com", "hashed-password"));
        var request = new RegisterUserRequest("Outra Pessoa", "ana@example.com", "outrasenha");

        var actionResult = await controller.Register(request, CancellationToken.None);

        var objectResult = Assert.IsType<ConflictObjectResult>(actionResult);
        var body = Assert.IsType<ConflictErrorResponse>(objectResult.Value);
        Assert.Equal("Já existe um usuário cadastrado com este e-mail.", body.Error);
    }

    [Fact]
    public async Task Register_WithPasswordShorterThan8Characters_Returns400WithErrors()
    {
        var controller = CreateController(out _);
        var request = new RegisterUserRequest("Ana Souza", "ana@example.com", "1234567");

        var actionResult = await controller.Register(request, CancellationToken.None);

        var objectResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var body = Assert.IsType<ValidationErrorResponse>(objectResult.Value);
        Assert.Single(body.Errors);
    }

    [Fact]
    public async Task Register_WithInvalidEmailFormat_Returns400WithErrors()
    {
        var controller = CreateController(out _);
        var request = new RegisterUserRequest("Ana Souza", "not-an-email", "senha1234");

        var actionResult = await controller.Register(request, CancellationToken.None);

        var objectResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var body = Assert.IsType<ValidationErrorResponse>(objectResult.Value);
        Assert.Single(body.Errors);
    }
}
