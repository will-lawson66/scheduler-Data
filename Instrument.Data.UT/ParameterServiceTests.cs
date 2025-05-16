using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;
using Instrument.Data.Exceptions;
using Instrument.Data.Repository;
using Instrument.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Instrument.Data.UT;

public class ParameterServiceTests : IDisposable
{
    private readonly SchedulerDbContext _dbContext;
    private readonly IParameterRepository _parameterRepository;
    private readonly Mock<ILogger<ParameterService>> _mockLogger;
    private readonly ParameterService _service;
    private readonly string _dbName;

    public ParameterServiceTests()
    {
        // Create a unique database name for each test run to ensure isolation
        _dbName = $"TestDb_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: _dbName)
            .Options;
        
        _dbContext = new SchedulerDbContext(options);

        // Set up repositories with in-memory database
        _parameterRepository = new ParameterRepository(_dbContext);
        
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
        var parameter = new Parameter 
        { 
            Name = "Test Parameter", 
            Type = ParameterType.StringType
        };

        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetParameterByIdAsync(parameter.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(parameter.Id, result.Id);
        Assert.Equal("Test Parameter", result.Name);
        Assert.Equal(ParameterType.StringType, result.Type);
    }
    
    [Fact]
    public async Task GetParameterAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _service.GetParameterByIdAsync(-2);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateParameterAsync_WithValidParameter_CreatesParameter()
    {
        // Arrange
        var parameter = new Parameter 
        { 
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
    public async Task UpdateParameterAsync_WithValidParameter_UpdatesParameter()
    {
        // Arrange
        var existingParameter = new Parameter 
        { 
            Name = "Original Parameter", 
            Type = ParameterType.StringType
        };
        
        await _dbContext.Parameters.AddAsync(existingParameter);
        await _dbContext.SaveChangesAsync();

        var updatedParameter = existingParameter.Update(name: "Updated parameter", ParameterType.IntegerType);

        // Act
        await _service.UpdateParameterAsync(updatedParameter);

        // Assert
        var resultParameter = await _dbContext.Parameters.FindAsync(existingParameter.Id);
        Assert.NotNull(resultParameter);
        Assert.Equal("Updated parameter", resultParameter.Name);
        Assert.Equal(ParameterType.IntegerType, resultParameter.Type);
    }

    [Fact]
    public async Task DeleteParameterAsync_DeletesParameter()
    {
        // Arrange
        var existingParameter = new Parameter 
        { 
            Name = "Parameter to Delete", 
            Type = ParameterType.StringType
        };
        
        await _dbContext.Parameters.AddAsync(existingParameter);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.DeleteParameterAsync(existingParameter.Id);

        // Assert
        var deletedParameter = await _dbContext.Parameters.FindAsync(existingParameter.Id);
        Assert.Null(deletedParameter);
    }

    [Fact]
    public async Task DeleteParameterAsync_WithNonExistingId_ThrowsEntityNotFoundException()
    {
        // Arrange
        var id = -5;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.DeleteParameterAsync(id));

        Assert.Equal<int>(id, exception.EntityId);
        Assert.Equal("Parameter", exception.EntityType);
    }

    // Validation tests
    
    [Fact]
    public void ValidateParameterValue_WithNullParameter_ThrowsArgumentNullException()
    {
        // Arrange
        Parameter? parameter = null;
        var value = "test";
        
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
            Name = "Number Parameter",
            Type = ParameterType.IntegerType,
            Min = "0",
            Max = "100"
        };
        
        var stringParam = new Parameter
        {
            Name = "String Parameter",
            Type = ParameterType.StringType,
            Min = "3",
            Max = "20"
        };
        
        var boolParam = new Parameter
        {
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
