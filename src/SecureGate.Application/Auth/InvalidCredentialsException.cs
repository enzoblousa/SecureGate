namespace SecureGate.Application.Auth;

public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("E-mail ou senha inválidos.")
    {
    }
}
