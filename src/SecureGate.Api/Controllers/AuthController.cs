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

    public AuthController(RegisterUserService registerUserService)
    {
        _registerUserService = registerUserService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ConflictErrorResponse), StatusCodes.Status409Conflict)]
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
            return Conflict(new ConflictErrorResponse(ex.Message));
        }
    }
}
