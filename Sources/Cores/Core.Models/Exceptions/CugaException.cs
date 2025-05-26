namespace Core.Models.Exceptions;

public sealed class CugaException : Exception
{
    public CugaException()
    {
    }

    public CugaException(string message) : base(message)
    {
    }

    public CugaException(string message, Exception innerException) : base(message, innerException)
    {
    }
}