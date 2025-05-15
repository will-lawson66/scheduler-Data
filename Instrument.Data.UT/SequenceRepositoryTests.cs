using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;
using Instrument.Data.Exceptions;
using Instrument.Data.Repository;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Data.UT;

public class SequenceRepositoryTests : IDisposable
{
    private readonly SchedulerDbContext _dbContext;
    private readonly SequenceRepository _repository;
    private readonly string _dbName;

    public SequenceRepositoryTests()
    {
        // Create a unique database name for each test run to ensure isolation
        _dbName = $"TestDB_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: _dbName)
            .Options;
        
        _dbContext = new SchedulerDbContext(options);
        _repository = new SequenceRepository(_dbContext);
    }

    public void Dispose()
    {
        // Clean up database after test
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
    
    [Fact]
    public async Task GetSequenceWithParametersAsync_ReturnsSequenceWithParameters()
    {
        // Arrange
        var sequence = new Sequence 
        { 
            Name = "Sequence 1", 
            Description = "Sequence description", 
            WorstCaseTime = TimeSpan.Zero
        };
        
        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();
        
        var parameter = new Parameter
        {
            Name = "Parameter 1",
            Type = ParameterType.StringType
        };
        
        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.SaveChangesAsync();
        
        var sequenceParameter = new SequenceParameter 
        { 
            SequenceId = sequence.Id, 
            ParameterId = parameter.Id, 
            OrderNumber = 1 
        };
        
        await _dbContext.SequenceParameters.AddAsync(sequenceParameter);
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetSequenceWithParametersAsync(sequence.Id);
        
        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.SequenceParameters);
        Assert.Single(result.SequenceParameters);
        Assert.Equal(parameter.Id, result.SequenceParameters.First().ParameterId);
    }
    
    [Fact]
    public async Task GetSequencesByPartialNameAsync_ReturnsMatchingSequences()
    {
        // Arrange
        await _dbContext.Sequences.AddRangeAsync(
            new Sequence { Name = "Alpha Sequence", Description = "Alpha description", WorstCaseTime = TimeSpan.Zero },
            new Sequence { Name = "Beta Sequence", Description = "Beta description", WorstCaseTime = TimeSpan.Zero },
            new Sequence { Name = "Gamma Sequence", Description = "Gamma description", WorstCaseTime = TimeSpan.Zero }
        );
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetSequencesByPartialNameAsync("Beta");
        
        // Assert
        Assert.NotNull(result);
        var collection = result.ToList();
        Assert.Single(collection);
        Assert.Equal("Beta Sequence", collection.First().Name);
    }
    
    [Fact]
    public async Task RemoveParameterFromSequenceAsync_RemovesParameter()
    {
        // Arrange
        var sequence = new Sequence 
        { 
            Name = "Test Sequence", 
            Description = "Test description", 
            WorstCaseTime = TimeSpan.Zero 
        };
        
        var parameter = new Parameter
        {
            Name = "Test Parameter",
            Type = Entities.Enums.ParameterType.StringType
        };

        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.SaveChangesAsync();

        var sequenceParameter = new SequenceParameter 
        { 
            SequenceId = sequence.Id, 
            ParameterId = parameter.Id, 
            OrderNumber = 1 
        };
        
        await _dbContext.SequenceParameters.AddAsync(sequenceParameter);
        await _dbContext.SaveChangesAsync();
        
        // Act
        await _repository.RemoveParameterFromSequenceAsync(parameter.Id, sequence.Id);
        
        // Assert
        var result = await _dbContext.SequenceParameters
            .FirstOrDefaultAsync(sp => sp.ParameterId == parameter.Id && sp.SequenceId == sequence.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task AddParameterToSequenceAsync_AddsSequenceParameter()
    {
        // Arrange
        // Create parameter and sequence
        var parameter = new Parameter { Name = "Parameter 1", Type = ParameterType.StringType };
        var sequence = new Sequence { Name = "Sequence 1", WorstCaseTime = TimeSpan.Zero };

        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();

        // Act
        await _repository.AddParameterToSequenceAsync(parameter.Id, sequence.Id);

        // Assert
        var sequenceParameter = await _dbContext.SequenceParameters
            .FirstOrDefaultAsync(sp => sp.ParameterId == parameter.Id && sp.SequenceId == sequence.Id);

        Assert.NotNull(sequenceParameter);
        Assert.Equal(0, sequenceParameter.OrderNumber);
    }

    [Fact]
    public async Task RemoveParameterFromSequenceAsync_RemovesSequenceParameter()
    {
        // Arrange
        // Create parameter and sequence
        var parameter = new Parameter { Name = "Parameter 1", Type = ParameterType.StringType };
        var sequence = new Sequence { Name = "Sequence 1", WorstCaseTime = TimeSpan.Zero };

        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();

        // Create association
        var sequenceParameter = new SequenceParameter
        {
            ParameterId = parameter.Id,
            SequenceId = sequence.Id,
            OrderNumber = 1
        };

        await _dbContext.SequenceParameters.AddAsync(sequenceParameter);
        await _dbContext.SaveChangesAsync();

        // Act
        await _repository.RemoveParameterFromSequenceAsync(parameter.Id, sequence.Id);

        // Assert
        var result = await _dbContext.SequenceParameters
            .FirstOrDefaultAsync(sp => sp.ParameterId == parameter.Id && sp.SequenceId == sequence.Id);

        Assert.Null(result);
    }
}
