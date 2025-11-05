namespace EMIS.BuildingBlocks.Exceptions;

/// <summary>
/// Exception thrown when a business rule is violated.
/// </summary>
public class BusinessRuleValidationException : DomainException
{
    public BusinessRuleValidationException(string message)
        : base(message)
    {
    }

    public BusinessRuleValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
