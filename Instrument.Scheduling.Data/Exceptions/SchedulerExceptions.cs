namespace Instrument.Scheduling.Data.Exceptions;

public class SchedulerDataException : Exception
{
    public SchedulerDataException(string message) : base(message) { }

    public SchedulerDataException(string message, Exception innerException)
        : base(message, innerException) { }
}

public class ParameterValidationException : SchedulerDataException
{
    public ParameterValidationException(string parameterId, string value, string reason)
        : base($"Invalid value '{value}' for parameter {parameterId}: {reason}") { }
}

public class StorageProviderException : SchedulerDataException
{
    public StorageProviderException(string operation, Exception innerException)
        : base($"Storage operation '{operation}' failed", innerException) { }
}

