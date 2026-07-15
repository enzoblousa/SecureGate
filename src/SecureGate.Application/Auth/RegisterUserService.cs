using Microsoft.Extensions.Logging;
using SecureGate.Application.Abstractions;
using SecureGate.Application.Auth.Dtos;
using SecureGate.Application.Events;
using SecureGate.Domain.Entities;

namespace SecureGate.Application.Auth;

public sealed class RegisterUserService
{
    private const int MinimumPasswordLength = 8;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<RegisterUserService> _logger;

    public RegisterUserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEventPublisher eventPublisher,
        ILogger<RegisterUserService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<RegisterUserResult> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.Password) || request.Password.Length < MinimumPasswordLength)
            throw new ArgumentException($"A senha deve ter no mínimo {MinimumPasswordLength} caracteres.");

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = new User(request.Name, request.Email, passwordHash);

        if (await _userRepository.GetByEmailAsync(user.Email, cancellationToken) is not null)
            throw new EmailAlreadyInUseException();

        await _userRepository.AddAsync(user, cancellationToken);

        await PublishUserRegisteredEventAsync(user, cancellationToken);

        return new RegisterUserResult(user.Id, user.Name, user.Email, user.CreatedAt);
    }

    private async Task PublishUserRegisteredEventAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            var @event = new UserRegisteredEvent(user.Id, user.Name, user.Email, user.CreatedAt);
            await _eventPublisher.PublishAsync(@event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao publicar UserRegisteredEvent para o usuário {UserId}", user.Id);
        }
    }
}
