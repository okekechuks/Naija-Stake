namespace NaijaStake.Domain.ExceptionHandling;

/// <summary>
/// Base exception for domain-level exceptions.
/// </summary>
public class DomainException : Exception
{
    public string ErrorCode { get; }

    public DomainException(string message, string errorCode = "DOMAIN_ERROR") 
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public DomainException(string message, Exception innerException, string errorCode = "DOMAIN_ERROR")
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Exception thrown when a business rule is violated.
/// </summary>
public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message)
        : base(message, "BUSINESS_RULE_VIOLATION") { }
}

/// <summary>
/// Exception thrown when a resource is not found.
/// </summary>
public class ResourceNotFoundException : DomainException
{
    public ResourceNotFoundException(string resourceName, string identifier)
        : base($"{resourceName} not found: {identifier}", "RESOURCE_NOT_FOUND") { }
}

/// <summary>
/// Exception thrown when trying to perform an invalid state transition.
/// </summary>
public class InvalidStateTransitionException : DomainException
{
    public InvalidStateTransitionException(string currentState, string targetState)
        : base($"Cannot transition from {currentState} to {targetState}.", "INVALID_STATE_TRANSITION") { }
}

/// <summary>
/// Exception thrown when insufficient funds.
/// </summary>
public class InsufficientFundsException : DomainException
{
    public InsufficientFundsException(decimal required, decimal available)
        : base($"Insufficient funds. Required: {required}, Available: {available}", "INSUFFICIENT_FUNDS") { }
}

/// <summary>
/// Exception thrown for concurrency/race condition issues.
/// </summary>
public class ConcurrencyException : DomainException
{
    public ConcurrencyException(string message)
        : base(message, "CONCURRENCY_ERROR") { }
}
