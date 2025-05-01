using Instrument.Scheduling.Data.Entities.Enums;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Instrument.Scheduling.Data.UT;

public class ParameterServiceTests
{
    private readonly Mock<IParameterRepository> _mockParameterRepository;
    private readonly Mock<ISequenceRepository> _mockSequenceRepository;
    private readonly Mock<ILogger<ParameterService>> _mockLogger;
    private readonly ParameterService _service;

    public ParameterServiceTests()
    {
        // Set up mocks
        _mockParameterRepository = new Mock<IParameterRepository>();
        _mockSequenceRepository = new Mock<ISequenceRepository>();
        _mockLogger = new Mock<ILogger<ParameterService>>();

        // Configure UnitOfWork mock to return our repository mocks

        // Create the service
        _service = new ParameterService(
            _mockParameterRepository.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetParameterAsync_WithValidId_ReturnsParameter()
    {
        // Arrange
        var id = "test-param-1";
        var parameter = new Parameter 
        { 
            Id = id, 
            Name = "Test Parameter", 
            Type = ParameterType.StringType
        };

        _mockParameterRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync(parameter);

        // Act
        var result = await _service.GetParameterAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Test Parameter", result.Name);
        Assert.Equal(ParameterType.StringType, result.Type);
    }

    [Fact]
    public async Task CreateParameterAsync_WithValidParameter_CreatesParameter()
    {
        // Arrange
        var parameter = new Parameter 
        { 
            Id = "test-param-1", 
            Name = "Test Parameter", 
            Type = ParameterType.StringType
        };

        _mockParameterRepository.Setup(repo => repo.GetByIdAsync(parameter.Id))
            .ReturnsAsync((Parameter?)null);

        // Act
        await _service.CreateParameterAsync(parameter);

        // Assert
        _mockParameterRepository.Verify(repo => repo.AddAsync(parameter), Times.Once);
    }

    [Fact]
    public async Task CreateParameterAsync_WithExistingId_ThrowsSchedulerDataException()
    {
        // Arrange
        var id = "test-param-1";
        var parameter = new Parameter 
        { 
            Id = id, 
            Name = "Test Parameter", 
            Type = ParameterType.StringType
        };
        var existingParameter = new Parameter 
        { 
            Id = id, 
            Name = "Existing Parameter", 
            Type = ParameterType.IntegerType
        };

        _mockParameterRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync(existingParameter);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SchedulerDataException>(() => 
            _service.CreateParameterAsync(parameter));
            
        Assert.Contains(id, exception.Message);
        
        _mockParameterRepository.Verify(repo => repo.AddAsync(It.IsAny<Parameter>()), Times.Never);
    }

    [Fact]
    public async Task UpdateParameterAsync_WithValidParameter_UpdatesParameter()
    {
        // Arrange
        var id = "test-param-1";
        var parameter = new Parameter 
        { 
            Id = id, 
            Name = "Updated Parameter", 
            Type = ParameterType.IntegerType
        };
        var existingParameter = new Parameter 
        { 
            Id = id, 
            Name = "Original Parameter", 
            Type = ParameterType.StringType
        };

        _mockParameterRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync(existingParameter);

        // Act
        await _service.UpdateParameterAsync(parameter);

        // Assert
        _mockParameterRepository.Verify(repo => repo.UpdateAsync(parameter), Times.Once);
    }

    [Fact]
    public async Task UpdateParameterAsync_WithNonExistingId_ThrowsEntityNotFoundException()
    {
        // Arrange
        var id = "test-param-1";
        var parameter = new Parameter 
        { 
            Id = id, 
            Name = "Test Parameter", 
            Type = ParameterType.StringType
        };

        _mockParameterRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Parameter?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.UpdateParameterAsync(parameter));
            
        Assert.Equal(id, exception.EntityId);
        Assert.Equal("Parameter", exception.EntityType);
        
        _mockParameterRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Parameter>()), Times.Never);
    }

    [Fact]
    public async Task DeleteParameterAsync_WithValidId_DeletesParameter()
    {
        // Arrange
        var id = "test-param-1";
        var existingParameter = new Parameter 
        { 
            Id = id, 
            Name = "Parameter to Delete", 
            Type = ParameterType.StringType
        };

        _mockParameterRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync(existingParameter);

        // Act
        await _service.DeleteParameterAsync(id);

        // Assert
        _mockParameterRepository.Verify(repo => repo.DeleteAsync(id), Times.Once);
    }

    [Fact]
    public async Task DeleteParameterAsync_WithNonExistingId_ThrowsEntityNotFoundException()
    {
        // Arrange
        var id = "test-param-1";

        _mockParameterRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Parameter?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.DeleteParameterAsync(id));
            
        Assert.Equal(id, exception.EntityId);
        Assert.Equal("Parameter", exception.EntityType);
        
        _mockParameterRepository.Verify(repo => repo.DeleteAsync(id), Times.Never);
    }

    [Fact]
    public async Task AddParameterToSequenceAsync_WithValidIds_AddsParameterToSequence()
    {
        // Arrange
        string sequenceId = "seq-1";
        string parameterId = "param-1";
        int orderNumber = 1;
        
        var parameter = new Parameter { Id = parameterId, Name = "Test Parameter", Type = ParameterType.StringType};
        var sequence = new Sequence { Id = sequenceId, Name = "Test Sequence", WorstCaseTime = TimeSpan.FromMilliseconds(30000)};
        
        _mockParameterRepository.Setup(repo => repo.GetByIdAsync(parameterId))
            .ReturnsAsync(parameter);
        
        _mockSequenceRepository.Setup(repo => repo.GetByIdAsync(sequenceId))
            .ReturnsAsync(sequence);

        // Act
        await _service.AddParameterToSequenceAsync(sequenceId, parameterId, orderNumber);

        // Assert
        _mockParameterRepository.Verify(repo => 
            repo.AddParameterToSequenceAsync(sequenceId, parameterId, orderNumber), Times.Once);
    }
    
    [Fact]
    public async Task AddParameterToSequenceAsync_WithInvalidParameterId_ThrowsEntityNotFoundException()
    {
        // Arrange
        string sequenceId = "seq-1";
        string parameterId = "invalid-param";
        int orderNumber = 1;
        
        var sequence = new Sequence { Id = sequenceId, Name = "Test Sequence", WorstCaseTime = TimeSpan.FromMilliseconds(20000)};
        
        _mockParameterRepository.Setup(repo => repo.GetByIdAsync(parameterId))
            .ReturnsAsync((Parameter?)null);
        
        _mockSequenceRepository.Setup(repo => repo.GetByIdAsync(sequenceId))
            .ReturnsAsync(sequence);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.AddParameterToSequenceAsync(sequenceId, parameterId, orderNumber));
            
        Assert.Equal(parameterId, exception.EntityId);
        Assert.Equal("Parameter", exception.EntityType);
    }
    
    [Fact]
    public async Task AddParameterToSequenceAsync_WithInvalidSequenceId_ThrowsEntityNotFoundException()
    {
        // Arrange
        string sequenceId = "invalid-seq";
        string parameterId = "param-1";
        int orderNumber = 1;
        
        var parameter = new Parameter { Id = parameterId, Name = "Test Parameter", Type = ParameterType.StringType};
        
        _mockParameterRepository.Setup(repo => repo.GetByIdAsync(parameterId))
            .ReturnsAsync(parameter);
        
        _mockSequenceRepository.Setup(repo => repo.GetByIdAsync(sequenceId))
            .ReturnsAsync((Sequence?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.AddParameterToSequenceAsync(sequenceId, parameterId, orderNumber));
            
        Assert.Equal(sequenceId, exception.EntityId);
        Assert.Equal("Sequence", exception.EntityType);
    }

    // Validation tests
    
    [Fact]
    public void ValidateParameterValue_WithNullParameter_ThrowsArgumentNullException()
    {
        // Arrange
        Parameter? parameter = null;
        string value = "test";
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _service.ValidateParameterValue(parameter!, value));
    }
    
    [Fact]
    public void ValidateParameterValue_WithValidValues_DoesNotThrow()
    {
        // Arrange
        var numberParam = new Parameter
        {
            Id = "num-param",
            Name = "Number Parameter",
            Type = ParameterType.IntegerType,
            Min = "0",
            Max = "100"
        };
        
        var stringParam = new Parameter
        {
            Id = "str-param",
            Name = "String Parameter",
            Type = ParameterType.StringType,
            Min = "3",
            Max = "20"
        };
        
        var boolParam = new Parameter
        {
            Id = "bool-param",
            Name = "Boolean Parameter",
            Type = ParameterType.BooleanType
        };
        
        // Act & Assert - all should not throw
        var exception1 = Record.Exception(() => 
            _service.ValidateParameterValue(numberParam, "50"));
        
        var exception2 = Record.Exception(() => 
            _service.ValidateParameterValue(stringParam, "Valid string"));
        
        var exception3 = Record.Exception(() => 
            _service.ValidateParameterValue(boolParam, "true"));
        
        Assert.Null(exception1);
        Assert.Null(exception2);
        Assert.Null(exception3);
    }
    
    [Fact]
    public void TryValidateParameterValue_ReturnsCorrectResults()
    {
        // Arrange
        var parameter = new Parameter
        {
            Id = "param-1",
            Name = "Test Parameter",
            Type = ParameterType.IntegerType,
            Min = "0",
            Max = "100"
        };
        
        // Act & Assert - valid value
        var result1 = _service.TryValidateParameterValue(parameter, "50");
        Assert.True(result1);
        
        // Act & Assert - invalid value
        var result2 = _service.TryValidateParameterValue(parameter, "invalid");
        Assert.False(result2);
        
        // Act & Assert - out of range
        var result3 = _service.TryValidateParameterValue(parameter, "150");
        Assert.False(result3);
    }
}
