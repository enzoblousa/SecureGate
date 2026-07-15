namespace SecureGate.Application.Auth;

public sealed class EmailAlreadyInUseException : Exception
{
    public EmailAlreadyInUseException()
        : base("Já existe um usuário cadastrado com este e-mail.")
    {
    }
}
