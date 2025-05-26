namespace Core.Models.Exceptions;

public sealed class AlgorithmException : Exception
{
    public AlgorithmException()
    {
    }

    public AlgorithmException(string message) : base(message)
    {
    }

    public AlgorithmException(Exception innerException) : base(string.Empty, innerException)
    {
    }

    public AlgorithmException(string message, Exception innerException) : base(message, innerException)
    {
    }
}