namespace Local.SQL.DB.Providers.Models.Exceptions;

public sealed class LoginException : Exception
{
    public LoginException(string message) : base(message)
    {
    }

    public LoginException(string message, Exception innerException) : base(message, innerException)
    {
    }
}