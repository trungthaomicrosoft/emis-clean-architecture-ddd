namespace EMIS.BuildingBlocks.Exceptions;

/// <summary>
/// Base exception for domain-specific exceptions.
/// </summary>
public class DomainException : Exception
{
    public DomainException()
    { }

    public DomainException(string message)
        : base(message)
    { }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
