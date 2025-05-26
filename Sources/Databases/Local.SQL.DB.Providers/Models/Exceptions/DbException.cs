namespace Local.SQL.DB.Providers.Models.Exceptions;

public sealed class DbException : Exception
{
    public DbException()
    {
    }

    public DbException(string message) : base(message)
    {
    }

    public DbException(string message, Exception innerException) : base(message, innerException)
    {
    }
}