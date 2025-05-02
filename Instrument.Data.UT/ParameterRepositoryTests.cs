using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;
using Instrument.Data.Exceptions;
using Instrument.Data.Repository;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Data.UT;

public class ParameterRepositoryTests : IDisposable
{
    private readonly SchedulerDbContext _dbContext;
    private readonly ParameterRepository _repository;
    private readonly string _dbName;

    public ParameterRepositoryTests()
    {
        // Create a unique database name for each test run to ensure isolation
        _dbName = $"TestDB_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: _dbName)
            .Options;
        
        _dbContext = new SchedulerDbContext(options);
        _repository = new ParameterRepository(_dbContext);
    }

    public void Dispose()
    {
        // Clean up database after test
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
    
    [Fact]
    public async Task GetAllAsync_ReturnsAllParameters()
    {
        // Arrange
        await _dbContext.Parameters.AddRangeAsync(
            new Parameter { Id = "param1", Name = "Parameter 1", Type = ParameterType.StringType },
            new Parameter { Id = "param2", Name = "Parameter 2", Type = ParameterType.IntegerType }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        var parameters = result.ToList();
        Assert.Equal(2, parameters.Count);
        Assert.Contains(parameters, p => p.Id == "param1");
        Assert.Contains(parameters, p => p.Id == "param2");
    }
    
    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsParameter()
    {
        // Arrange
        var parameter = new Parameter
        { 
            Id = "param1",
            Name = "Parameter 1", 
            Type = ParameterType.StringType
        };
        
        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync("param1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("param1", result.Id);
        Assert.Equal("Parameter 1", result.Name);
        Assert.Equal(ParameterType.StringType, result.Type);
    }
    
    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync("non-existent");
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetQueryableAsync_ReturnsQueryable()
    {
        // Arrange
        await _dbContext.Parameters.AddRangeAsync(
            new Parameter { Id = "param1", Name = "Parameter 1", Type = ParameterType.StringType },
            new Parameter { Id = "param2", Name = "Parameter 2", Type = ParameterType.IntegerType }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetQueryableAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IQueryable<Parameter>>(result);
        Assert.Equal(2, result.Count());
    }
    
    [Fact]
    public async Task AddAsync_AddsParameter()
    {
        // Arrange
        var parameter = new Parameter
        { 
            Id = "new-id", 
            Name = "New Parameter",
            Type = ParameterType.StringType
        };
        
        // Act
        await _repository.AddAsync(parameter);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _dbContext.Parameters.FindAsync("new-id");
        Assert.NotNull(result);
        Assert.Equal("New Parameter", result.Name);
        Assert.Equal(ParameterType.StringType, result.Type);
    }
    
    [Fact]
    public async Task UpdateAsync_UpdatesParameter()
    {
        // Arrange
        var original = new Parameter
        { 
            Id = "param-id", 
            Name = "Original Parameter",
            Type = ParameterType.StringType
        };
        
        await _dbContext.Parameters.AddAsync(original);
        await _dbContext.SaveChangesAsync();
        
        var updated = new Parameter
        { 
            Id = "param-id", 
            Name = "Updated Parameter",
            Type = ParameterType.IntegerType
        };

        // Act
        await _repository.UpdateAsync(updated);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _dbContext.Parameters.FindAsync("param-id");
        Assert.NotNull(result);
        Assert.Equal("Updated Parameter", result.Name);
        Assert.Equal(ParameterType.IntegerType, result.Type);
    }
    
    [Fact]
    public async Task DeleteAsync_DeletesParameter()
    {
        // Arrange
        var parameter = new Parameter
        { 
            Id = "delete-id", 
            Name = "Delete Parameter",
            Type = ParameterType.StringType
        };
        
        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.SaveChangesAsync();
        
        // Act
        await _repository.DeleteAsync("delete-id");
        await _repository.SaveChangesAsync();
        
        // Assert
        var result = await _dbContext.Parameters.FindAsync("delete-id");
        Assert.Null(result);
    }
    
    [Fact]
    public async Task DeleteAsync_WithInvalidId_ThrowsEntityNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _repository.DeleteAsync("non-existent"));
    }
    
    [Fact]
    public async Task GetParametersForSequenceAsync_ReturnsCorrectParameters()
    {
        // Arrange
        string sequenceId = "seq1";
        
        // Create parameters
        var param1 = new Parameter { Id = "param1", Name = "Parameter 1", Type = ParameterType.StringType };
        var param2 = new Parameter { Id = "param2", Name = "Parameter 2", Type = ParameterType.IntegerType };
        var param3 = new Parameter { Id = "param3", Name = "Parameter 3", Type = ParameterType.BooleanType };
        
        // Create a sequence
        var sequence = new Sequence { Id = sequenceId, Name = "Test Sequence", WorstCaseTime = TimeSpan.FromSeconds(30) };
        
        // Create sequence parameters (relationships)
        var seqParam1 = new SequenceParameter { SequenceId = sequenceId, ParameterId = "param1", OrderNumber = 1 };
        var seqParam2 = new SequenceParameter { SequenceId = sequenceId, ParameterId = "param2", OrderNumber = 2 };
        
        // Add to database
        await _dbContext.Parameters.AddRangeAsync(param1, param2, param3);
        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();
        
        await _dbContext.SequenceParameters.AddRangeAsync(seqParam1, seqParam2);
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetParametersForSequenceAsync(sequenceId);
        
        // Assert
        var parameters = result.ToList();
        Assert.Equal(2, parameters.Count);
        Assert.Contains(parameters, p => p.Id == "param1");
        Assert.Contains(parameters, p => p.Id == "param2");
        Assert.DoesNotContain(parameters, p => p.Id == "param3");
    }
    
    [Fact]
    public async Task AddParameterToSequenceAsync_AddsSequenceParameter()
    {
        // Arrange
        string parameterId = "param1";
        string sequenceId = "seq1";
        int orderNumber = 1;
        
        // Create parameter and sequence
        var parameter = new Parameter { Id = parameterId, Name = "Parameter 1", Type = ParameterType.StringType };
        var sequence = new Sequence { Id = sequenceId, Name = "Sequence 1", WorstCaseTime = TimeSpan.Zero };
        
        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();
        
        // Act
        await _repository.AddParameterToSequenceAsync(parameterId, sequenceId, orderNumber);
        
        // Assert
        var sequenceParameter = await _dbContext.SequenceParameters
            .FirstOrDefaultAsync(sp => sp.ParameterId == parameterId && sp.SequenceId == sequenceId);
        
        Assert.NotNull(sequenceParameter);
        Assert.Equal(orderNumber, sequenceParameter.OrderNumber);
    }
    
    [Fact]
    public async Task RemoveParameterFromSequenceAsync_RemovesSequenceParameter()
    {
        // Arrange
        string parameterId = "param1";
        string sequenceId = "seq1";
        
        // Create parameter and sequence
        var parameter = new Parameter { Id = parameterId, Name = "Parameter 1", Type = ParameterType.StringType };
        var sequence = new Sequence { Id = sequenceId, Name = "Sequence 1", WorstCaseTime = TimeSpan.Zero };
        
        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();
        
        // Create association
        var sequenceParameter = new SequenceParameter
        {
            ParameterId = parameterId,
            SequenceId = sequenceId,
            OrderNumber = 1
        };
        
        await _dbContext.SequenceParameters.AddAsync(sequenceParameter);
        await _dbContext.SaveChangesAsync();
        
        // Act
        await _repository.RemoveParameterFromSequenceAsync(parameterId, sequenceId);
        
        // Assert
        var result = await _dbContext.SequenceParameters
            .FirstOrDefaultAsync(sp => sp.ParameterId == parameterId && sp.SequenceId == sequenceId);
        
        Assert.Null(result);
    }
}
