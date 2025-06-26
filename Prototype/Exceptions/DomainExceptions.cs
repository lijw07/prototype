namespace Prototype.Exceptions;

/// <summary>
/// Base class for all domain-specific exceptions
/// </summary>
public abstract class DomainException : Exception
{
    public abstract string UserMessage { get; }

    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : DomainException
{
    public List<string> Errors { get; }
    public override string UserMessage => "Validation failed";

    public ValidationException(string error) : base(error)
    {
        Errors = new List<string> { error };
    }

    public ValidationException(List<string> errors) : base("Validation failed")
    {
        Errors = errors ?? new List<string>();
    }

    public ValidationException(string message, List<string> errors) : base(message)
    {
        Errors = errors ?? new List<string>();
    }
}

/// <summary>
/// Exception thrown when a business rule is violated
/// </summary>
public class BusinessRuleException : DomainException
{
    public override string UserMessage { get; }

    public BusinessRuleException(string message, string? userMessage = null) : base(message)
    {
        UserMessage = userMessage ?? message;
    }
}

/// <summary>
/// Exception thrown when a resource is not found
/// </summary>
public class NotFoundException : DomainException
{
    public override string UserMessage => "The requested resource was not found";
    public string ResourceType { get; }
    public string ResourceId { get; }

    public NotFoundException(string resourceType, string resourceId) 
        : base($"{resourceType} with ID '{resourceId}' was not found")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public NotFoundException(string message) : base(message)
    {
        ResourceType = "Resource";
        ResourceId = "Unknown";
    }
}

/// <summary>
/// Exception thrown when there's a conflict with existing data
/// </summary>
public class ConflictException : DomainException
{
    public override string UserMessage => "A conflict occurred with existing data";

    public ConflictException(string message) : base(message) { }
    public ConflictException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when access is forbidden
/// </summary>
public class ForbiddenException : DomainException
{
    public override string UserMessage => "Access to this resource is forbidden";

    public ForbiddenException(string message) : base(message) { }
    public ForbiddenException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when external services fail
/// </summary>
public class ExternalServiceException : Exception
{
    public string ServiceName { get; }
    public string? ServiceResponse { get; }

    public ExternalServiceException(string serviceName, string message) : base(message)
    {
        ServiceName = serviceName;
    }

    public ExternalServiceException(string serviceName, string message, string? serviceResponse) : base(message)
    {
        ServiceName = serviceName;
        ServiceResponse = serviceResponse;
    }

    public ExternalServiceException(string serviceName, string message, Exception innerException) 
        : base(message, innerException)
    {
        ServiceName = serviceName;
    }
}

/// <summary>
/// Exception thrown during database operations
/// </summary>
public class DatabaseException : Exception
{
    public string Operation { get; }
    public string? TableName { get; }

    public DatabaseException(string operation, string message) : base(message)
    {
        Operation = operation;
    }

    public DatabaseException(string operation, string message, string? tableName) : base(message)
    {
        Operation = operation;
        TableName = tableName;
    }

    public DatabaseException(string operation, string message, Exception innerException) 
        : base(message, innerException)
    {
        Operation = operation;
    }
}

/// <summary>
/// Exception thrown when bulk operations fail
/// </summary>
public class BulkOperationException : Exception
{
    public int TotalRecords { get; }
    public int ProcessedRecords { get; }
    public int FailedRecords { get; }
    public List<string> Errors { get; }

    public BulkOperationException(
        int totalRecords, 
        int processedRecords, 
        int failedRecords, 
        List<string> errors,
        string message) : base(message)
    {
        TotalRecords = totalRecords;
        ProcessedRecords = processedRecords;
        FailedRecords = failedRecords;
        Errors = errors ?? new List<string>();
    }
}