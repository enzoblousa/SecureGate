using Microsoft.AspNetCore.Mvc;
using SecureGate.Api.Controllers.Contracts;
using SecureGate.Application.Auth;
using SecureGate.Application.Auth.Dtos;

namespace SecureGate.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly RegisterUserService _registerUserService;
    private readonly LoginService _loginService;

    public AuthController(RegisterUserService registerUserService, LoginService loginService)
    {
        _registerUserService = registerUserService;
        _loginService = loginService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _registerUserService.RegisterAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ValidationErrorResponse(new[] { ex.Message }));
        }
        catch (EmailAlreadyInUseException ex)
        {
            return Conflict(new ErrorResponse(ex.Message));
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _loginService.LoginAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ValidationErrorResponse(new[] { ex.Message }));
        }
        catch (InvalidCredentialsException ex)
        {
            return Unauthorized(new ErrorResponse(ex.Message));
        }
    }
}
