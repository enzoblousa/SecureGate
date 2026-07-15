using System.Net.Mail;

namespace SecureGate.Domain;

public static class EmailValidator
{
    public static bool IsValidFormat(string email)
    {
        try
        {
            return new MailAddress(email).Address == email;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
