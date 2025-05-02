using Instrument.Data.Exceptions;

namespace Instrument.Data.UT;
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
    public void ValidationException_ConstructsCorrectly_WithParameterIdValueAndReason()
    {
        // Arrange
        string parameterId = "param-123";
        string value = "invalid-value";
        string reason = "Value must be a number";
        
        // Act
        var exception = new ValidationException(parameterId, value, reason);
        
        // Assert
        Assert.Contains(parameterId, exception.Message);
        Assert.Contains(value, exception.Message);
        Assert.Contains(reason, exception.Message);
        Assert.Equal(value, exception.Value);
        Assert.Equal(reason, exception.Reason);
        Assert.Null(exception.InnerException);
    }
    
    [Fact]
    public void StorageProviderException_ConstructsCorrectly_WithOperationAndInnerException()
    {
        // Arrange
        string operation = "GetAll()";
        var innerException = new IOException("Inner exception");
        
        // Act
        var exception = new StorageProviderException(operation, innerException);
        
        // Assert
        Assert.Contains(operation, exception.Message);
        Assert.Same(innerException, exception.InnerException);
        Assert.Equal(operation, exception.Operation);
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
        Assert.Equal(entityType, exception.EntityType);
        Assert.Equal(entityId, exception.EntityId);
        Assert.Null(exception.InnerException);
    }
    
    [Fact]
    public void Exceptions_InheritFromSchedulerDataException()
    {
        // Arrange & Act
        var validationException = new ValidationException("param-123", "invalid", "Test reason");
        var storageProviderException = new StorageProviderException("GetAll", new Exception());
        var entityNotFoundException = new EntityNotFoundException("Entity", "id-123");
        
        // Assert
        Assert.IsAssignableFrom<SchedulerDataException>(validationException);
        Assert.IsAssignableFrom<SchedulerDataException>(storageProviderException);
        Assert.IsAssignableFrom<SchedulerDataException>(entityNotFoundException);
    }
}
