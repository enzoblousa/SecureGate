using SecureGate.Domain;

namespace SecureGate.Domain.Entities;

public sealed class User
{
    public Guid Id { get; }
    public string Name { get; }
    public string Email { get; }
    public string PasswordHash { get; }
    public DateTime CreatedAt { get; }

    public User(string name, string email, string passwordHash)
    {
        Name = ValidateName(name);
        Email = ValidateEmail(email);
        PasswordHash = ValidatePasswordHash(passwordHash);
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    // Reservado para o EF Core materializar entidades vindas do banco via
    // acesso direto aos backing fields, sem repassar pelas validações acima.
    private User()
    {
        Id = Guid.Empty;
        Name = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
        CreatedAt = default;
    }

    private static string ValidateName(string name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException("O nome é obrigatório.");

        if (trimmed.Length < 2 || trimmed.Length > 100)
            throw new ArgumentException("O nome deve ter entre 2 e 100 caracteres.");

        return trimmed;
    }

    private static string ValidateEmail(string email)
    {
        var trimmed = email?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException("O e-mail é obrigatório.");

        if (!EmailValidator.IsValidFormat(trimmed))
            throw new ArgumentException("O e-mail informado é inválido.");

        return trimmed;
    }

    private static string ValidatePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("O hash de senha é obrigatório.");

        return passwordHash;
    }
}
