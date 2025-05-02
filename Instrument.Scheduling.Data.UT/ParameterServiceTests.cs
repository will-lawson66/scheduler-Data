using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Entities.Enums;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Repository;
using Instrument.Scheduling.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Instrument.Scheduling.Data.UT;

public class ParameterServiceTests : IDisposable
{
    private readonly SchedulerDbContext _dbContext;
    private readonly IParameterRepository _parameterRepository;
    private readonly ISequenceRepository _sequenceRepository;
    private readonly Mock<ILogger<ParameterService>> _mockLogger;
    private readonly ParameterService _service;

    public ParameterServiceTests()
    {
        // Set up in-memory database
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new SchedulerDbContext(options);

        // Set up real repositories with in-memory database
        _parameterRepository = new ParameterRepository(_dbContext);
        _sequenceRepository = new SequenceRepository(_dbContext);
        
        // Set up logger mock
        _mockLogger = new Mock<ILogger<ParameterService>>();

        // Create the service
        _service = new ParameterService(
            _parameterRepository,
            _mockLogger.Object
        );
    }
    
    public void Dispose()
    {
        // Clean up database after test
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
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

        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.SaveChangesAsync();

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

        // Act
        await _service.CreateParameterAsync(parameter);

        // Assert
        var createdParameter = await _dbContext.Parameters.FindAsync(parameter.Id);
        Assert.NotNull(createdParameter);
        Assert.Equal("Test Parameter", createdParameter.Name);
        Assert.Equal(ParameterType.StringType, createdParameter.Type);
    }

    [Fact]
    public async Task CreateParameterAsync_WithExistingId_ThrowsSchedulerDataException()
    {
        // Arrange
        var id = "test-param-1";
        var existingParameter = new Parameter 
        { 
            Id = id, 
            Name = "Existing Parameter", 
            Type = ParameterType.IntegerType
        };
        
        await _dbContext.Parameters.AddAsync(existingParameter);
        await _dbContext.SaveChangesAsync();

        var parameter = new Parameter 
        { 
            Id = id, 
            Name = "Test Parameter", 
            Type = ParameterType.StringType
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SchedulerDataException>(() => 
            _service.CreateParameterAsync(parameter));
            
        Assert.Contains(id, exception.Message);
        
        var unchangedParameter = await _dbContext.Parameters.FindAsync(id);
        Assert.Equal("Existing Parameter", unchangedParameter.Name);
        Assert.Equal(ParameterType.IntegerType, unchangedParameter.Type);
    }

    [Fact]
    public async Task UpdateParameterAsync_WithValidParameter_UpdatesParameter()
    {
        // Arrange
        var id = "test-param-1";
        var existingParameter = new Parameter 
        { 
            Id = id, 
            Name = "Original Parameter", 
            Type = ParameterType.StringType
        };
        
        await _dbContext.Parameters.AddAsync(existingParameter);
        await _dbContext.SaveChangesAsync();

        var updatedParameter = new Parameter 
        { 
            Id = id, 
            Name = "Updated Parameter", 
            Type = ParameterType.IntegerType
        };

        // Act
        await _service.UpdateParameterAsync(updatedParameter);

        // Assert
        var resultParameter = await _dbContext.Parameters.FindAsync(id);
        Assert.NotNull(resultParameter);
        Assert.Equal("Updated Parameter", resultParameter.Name);
        Assert.Equal(ParameterType.IntegerType, resultParameter.Type);
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

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.UpdateParameterAsync(parameter));
            
        Assert.Equal(id, exception.EntityId);
        Assert.Equal("Parameter", exception.EntityType);
        
        var parameterCount = await _dbContext.Parameters.CountAsync();
        Assert.Equal(0, parameterCount);
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
        
        await _dbContext.Parameters.AddAsync(existingParameter);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.DeleteParameterAsync(id);

        // Assert
        var deletedParameter = await _dbContext.Parameters.FindAsync(id);
        Assert.Null(deletedParameter);
    }

    [Fact]
    public async Task DeleteParameterAsync_WithNonExistingId_ThrowsEntityNotFoundException()
    {
        // Arrange
        var id = "test-param-1";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.DeleteParameterAsync(id));
            
        Assert.Equal(id, exception.EntityId);
        Assert.Equal("Parameter", exception.EntityType);
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
        
        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.AddParameterToSequenceAsync(sequenceId, parameterId, orderNumber);

        // Assert
        var association = await _dbContext.SequenceParameters
            .FirstOrDefaultAsync(sp => sp.ParameterId == parameterId && sp.SequenceId == sequenceId);
        Assert.NotNull(association);
        Assert.Equal(orderNumber, association.OrderNumber);
    }
    
    [Fact]
    public async Task AddParameterToSequenceAsync_WithInvalidParameterId_ThrowsEntityNotFoundException()
    {
        // Arrange
        string sequenceId = "seq-1";
        string parameterId = "invalid-param";
        int orderNumber = 1;
        
        var sequence = new Sequence { Id = sequenceId, Name = "Test Sequence", WorstCaseTime = TimeSpan.FromMilliseconds(20000)};
        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();

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
        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.SaveChangesAsync();

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
