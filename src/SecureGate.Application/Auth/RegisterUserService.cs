using SecureGate.Application.Abstractions;
using SecureGate.Application.Auth.Dtos;
using SecureGate.Domain.Entities;

namespace SecureGate.Application.Auth;

public sealed class RegisterUserService
{
    private const int MinimumPasswordLength = 8;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterUserResult> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.Password) || request.Password.Length < MinimumPasswordLength)
            throw new ArgumentException($"A senha deve ter no mínimo {MinimumPasswordLength} caracteres.", nameof(request.Password));

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = new User(request.Name, request.Email, passwordHash);

        if (await _userRepository.GetByEmailAsync(user.Email, cancellationToken) is not null)
            throw new EmailAlreadyInUseException();

        await _userRepository.AddAsync(user, cancellationToken);

        return new RegisterUserResult(user.Id, user.Name, user.Email, user.CreatedAt);
    }
}
