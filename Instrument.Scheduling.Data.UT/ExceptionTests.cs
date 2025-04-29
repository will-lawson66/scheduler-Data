using Instrument.Scheduling.Data.Exceptions;

namespace Instrument.Scheduling.Data.UT;
public class ExceptionTests
{
    [Fact]
    public void SchedulerDataException_ConstructsCorrectly_WithMessage()
    {
        // Arrange
        string message = "Test data exception message";
        
        // Act
        var exception = new SchedulerDataException(message);
        
        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }
    
    [Fact]
    public void SchedulerDataException_ConstructsCorrectly_WithMessageAndInnerException()
    {
        // Arrange
        string message = "Test data exception message";
        var innerException = new InvalidOperationException("Inner exception");
        
        // Act
        var exception = new SchedulerDataException(message, innerException);
        
        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }
    
    [Fact]
    public void StorageProviderException_ConstructsCorrectly_WithMessageAndInnerException()
    {
        // Arrange
        string operation = "GetAll()";
        var innerException = new IOException("Inner exception");
        
        // Act
        var exception = new StorageProviderException(operation, innerException);
        
        // Assert
        Assert.Contains(operation, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }
    
    [Fact]
    public void EntityNotFoundException_ConstructsCorrectly_WithEntityTypeAndId()
    {
        // Arrange
        string entityType = "Sequence";
        string entityId = "seq-123";
        
        // Act
        var exception = new EntityNotFoundException(entityType, entityId);
        
        // Assert
        Assert.Contains(entityType, exception.Message);
        Assert.Contains(entityId, exception.Message);
        Assert.Null(exception.InnerException);
    }
}