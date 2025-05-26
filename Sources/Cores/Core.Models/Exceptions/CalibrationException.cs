namespace Core.Models.Exceptions;

public sealed class CalibrationException : Exception
{
    public CalibrationException(string message) : base(message)
    {
    }

    public CalibrationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}