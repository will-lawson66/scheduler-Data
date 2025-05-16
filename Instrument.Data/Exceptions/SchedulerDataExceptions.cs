namespace Instrument.Data.Exceptions;

/// <summary>
/// Base exception type for all Scheduler Data layer exceptions
/// </summary>
public class SchedulerDataException : Exception
{
    public SchedulerDataException(string message) : base(message) { }

    public SchedulerDataException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : SchedulerDataException
{
    public string Value { get; }
    public string Reason { get; }

    public ValidationException(int parameterId, string value, string reason)
        : base($"Invalid value '{value}' for parameter {parameterId}: {reason}")
    {
        Value = value;
        Reason = reason;
    }
}

/// <summary>
/// Exception thrown when storage operations fail
/// </summary>
public class StorageProviderException : SchedulerDataException
{
    public string Operation { get; }

    public StorageProviderException(string operation, Exception innerException)
        : base($"Storage operation '{operation}' failed", innerException)
    {
        Operation = operation;
    }
}

/// <summary>
/// Exception thrown when a required entity is not found
/// </summary>
public class EntityNotFoundException : SchedulerDataException
{
    public string EntityType { get; }
    public int EntityId { get; }

    public EntityNotFoundException(string entityType, int entityId)
        : base($"{entityType} with ID '{entityId}' not found.")
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}