using SecureGate.Application.Abstractions;
using SecureGate.Application.Auth.Dtos;
using SecureGate.Domain;

namespace SecureGate.Application.Auth;

public sealed class LoginService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;

    public LoginService(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<TokenResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim();
        if (string.IsNullOrEmpty(email))
            throw new ArgumentException("O e-mail é obrigatório.");

        if (!EmailValidator.IsValidFormat(email))
            throw new ArgumentException("O e-mail informado é inválido.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("A senha é obrigatória.");

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        return _tokenGenerator.Generate(user);
    }
}
