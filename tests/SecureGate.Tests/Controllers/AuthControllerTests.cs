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
    private static AuthController CreateController(out FakeUserRepository repository, out FakePasswordHasher hasher)
    {
        repository = new FakeUserRepository();
        hasher = new FakePasswordHasher();
        var registerService = new RegisterUserService(repository, hasher);
        var loginService = new LoginService(repository, hasher, new FakeTokenGenerator());
        return new AuthController(registerService, loginService);
    }

    private static AuthController CreateController(out FakeUserRepository repository) =>
        CreateController(out repository, out _);

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
        var body = Assert.IsType<ErrorResponse>(objectResult.Value);
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

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200WithToken()
    {
        var controller = CreateController(out var repository, out var hasher);
        await repository.AddAsync(new User("Ana Souza", "ana@example.com", hasher.Hash("senha1234")));
        var request = new LoginRequest("ana@example.com", "senha1234");

        var actionResult = await controller.Login(request, CancellationToken.None);

        var objectResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.IsType<TokenResult>(objectResult.Value);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401WithGenericMessage()
    {
        var controller = CreateController(out _, out _);
        var request = new LoginRequest("nobody@example.com", "senha1234");

        var actionResult = await controller.Login(request, CancellationToken.None);

        var objectResult = Assert.IsType<UnauthorizedObjectResult>(actionResult);
        var body = Assert.IsType<ErrorResponse>(objectResult.Value);
        Assert.Equal("E-mail ou senha inválidos.", body.Error);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401WithGenericMessage()
    {
        var controller = CreateController(out var repository, out var hasher);
        await repository.AddAsync(new User("Ana Souza", "ana@example.com", hasher.Hash("senha1234")));
        var request = new LoginRequest("ana@example.com", "senhaerrada");

        var actionResult = await controller.Login(request, CancellationToken.None);

        var objectResult = Assert.IsType<UnauthorizedObjectResult>(actionResult);
        var body = Assert.IsType<ErrorResponse>(objectResult.Value);
        Assert.Equal("E-mail ou senha inválidos.", body.Error);
    }

    [Fact]
    public async Task Login_WithInvalidEmailFormat_Returns400WithErrors()
    {
        var controller = CreateController(out _, out _);
        var request = new LoginRequest("not-an-email", "senha1234");

        var actionResult = await controller.Login(request, CancellationToken.None);

        var objectResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var body = Assert.IsType<ValidationErrorResponse>(objectResult.Value);
        Assert.Single(body.Errors);
    }
}
