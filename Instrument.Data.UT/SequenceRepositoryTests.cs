using Instrument\.Data.DataContext;
using Instrument\.Data.Entities;
using Instrument\.Data.Exceptions;
using Instrument\.Data.Repository;
using Microsoft.EntityFrameworkCore;

namespace Instrument\.Data.UT;

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
    public async Task GetAllAsync_ReturnsAllSequences()
    {
        // Arrange
        await _dbContext.Sequences.AddRangeAsync(
            new Sequence { Id = "seq1", Name = "Sequence 1", Description = "Sequence description", WorstCaseTime = TimeSpan.Zero },
            new Sequence { Id = "seq2", Name = "Sequence 2", Description = "Sequence 2 description", WorstCaseTime = TimeSpan.Zero }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        var sequences = result.ToList();
        Assert.Equal(2, sequences.Count);
        Assert.Contains(sequences, s => s.Id == "seq1");
        Assert.Contains(sequences, s => s.Id == "seq2");
    }
    
    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsSequence()
    {
        // Arrange
        var sequence = new Sequence 
        { 
            Id = "seq-id",
            Name = "Test Sequence", 
            Description = "Test description", 
            WorstCaseTime = TimeSpan.Zero 
        };
        
        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync("seq-id");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("seq-id", result.Id);
        Assert.Equal("Test Sequence", result.Name);
        Assert.Equal("Test description", result.Description);
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
        await _dbContext.Sequences.AddRangeAsync(
            new Sequence { Id = "seq1", Name = "Sequence 1", Description = "Sequence description", WorstCaseTime = TimeSpan.Zero },
            new Sequence { Id = "seq2", Name = "Sequence 2", Description = "Sequence 2 description", WorstCaseTime = TimeSpan.Zero }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetQueryableAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IQueryable<Sequence>>(result);
        Assert.Equal(2, result.Count());
    }
    
    [Fact]
    public async Task AddAsync_AddsSequence()
    {
        // Arrange
        var sequence = new Sequence 
        { 
            Id = "new-id", 
            Name = "New Sequence",
            Description = "New description", 
            WorstCaseTime = TimeSpan.Zero 
        };
        
        // Act
        await _repository.AddAsync(sequence);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _dbContext.Sequences.FindAsync("new-id");
        Assert.NotNull(result);
        Assert.Equal("New Sequence", result.Name);
        Assert.Equal("New description", result.Description);
    }
    
    [Fact]
    public async Task UpdateAsync_UpdatesSequence()
    {
        // Arrange
        var original = new Sequence 
        { 
            Id = "seq-id", 
            Name = "Original Sequence",
            Description = "Original description", 
            WorstCaseTime = TimeSpan.Zero 
        };
        
        await _dbContext.Sequences.AddAsync(original);
        await _dbContext.SaveChangesAsync();
        
        var updated = new Sequence 
        { 
            Id = "seq-id", 
            Name = "Updated Sequence",
            Description = "Updated description", 
            WorstCaseTime = TimeSpan.FromSeconds(10) 
        };

        // Act
        await _repository.UpdateAsync(updated);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _dbContext.Sequences.FindAsync("seq-id");
        Assert.NotNull(result);
        Assert.Equal("Updated Sequence", result.Name);
        Assert.Equal("Updated description", result.Description);
        Assert.Equal(TimeSpan.FromSeconds(10), result.WorstCaseTime);
    }
    
    [Fact]
    public async Task DeleteAsync_DeletesSequence()
    {
        // Arrange
        var sequence = new Sequence 
        { 
            Id = "delete-id", 
            Name = "Delete Sequence",
            Description = "Delete description", 
            WorstCaseTime = TimeSpan.Zero 
        };
        
        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();
        
        // Act
        await _repository.DeleteAsync("delete-id");
        await _repository.SaveChangesAsync();
        
        // Assert
        var result = await _dbContext.Sequences.FindAsync("delete-id");
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
    public async Task GetSequenceWithParametersAsync_ReturnsSequenceWithParameters()
    {
        // Arrange
        var sequence = new Sequence 
        { 
            Id = "seq1", 
            Name = "Sequence 1", 
            Description = "Sequence description", 
            WorstCaseTime = TimeSpan.Zero
        };
        
        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();
        
        var parameter = new Parameter
        {
            Id = "param1",
            Name = "Parameter 1",
            Type = Entities.Enums.ParameterType.StringType
        };
        
        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.SaveChangesAsync();
        
        var sequenceParameter = new SequenceParameter 
        { 
            SequenceId = "seq1", 
            ParameterId = "param1", 
            OrderNumber = 1 
        };
        
        await _dbContext.SequenceParameters.AddAsync(sequenceParameter);
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetSequenceWithParametersAsync("seq1");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("seq1", result.Id);
        Assert.NotNull(result.SequenceParameters);
        Assert.Single(result.SequenceParameters);
        Assert.Equal("param1", result.SequenceParameters.First().ParameterId);
    }
    
    [Fact]
    public async Task GetSequencesByNameAsync_ReturnsMatchingSequences()
    {
        // Arrange
        await _dbContext.Sequences.AddRangeAsync(
            new Sequence { Id = "seq1", Name = "Alpha Sequence", Description = "Alpha description", WorstCaseTime = TimeSpan.Zero },
            new Sequence { Id = "seq2", Name = "Beta Sequence", Description = "Beta description", WorstCaseTime = TimeSpan.Zero },
            new Sequence { Id = "seq3", Name = "Gamma Sequence", Description = "Gamma description", WorstCaseTime = TimeSpan.Zero }
        );
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetSequencesByNameAsync("Beta");
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Beta Sequence", result.First().Name);
    }
    
    [Fact]
    public async Task RemoveParameterFromSequenceAsync_RemovesParameter()
    {
        // Arrange
        var parameterId = "param1";
        var sequenceId = "seq1";
        
        var sequence = new Sequence 
        { 
            Id = sequenceId, 
            Name = "Test Sequence", 
            Description = "Test description", 
            WorstCaseTime = TimeSpan.Zero 
        };
        
        var parameter = new Parameter
        {
            Id = parameterId,
            Name = "Test Parameter",
            Type = Entities.Enums.ParameterType.StringType
        };
        
        var sequenceParameter = new SequenceParameter 
        { 
            SequenceId = sequenceId, 
            ParameterId = parameterId, 
            OrderNumber = 1 
        };
        
        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.SaveChangesAsync();
        
        await _dbContext.SequenceParameters.AddAsync(sequenceParameter);
        await _dbContext.SaveChangesAsync();
        
        // Act
        await _repository.RemoveParameterFromSequenceAsync(parameterId, sequenceId);
        
        // Assert
        var result = await _dbContext.SequenceParameters
            .FirstOrDefaultAsync(sp => sp.ParameterId == parameterId && sp.SequenceId == sequenceId);
        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetSequencesByIdsAsync_ReturnsMatchingSequences()
    {
        // Arrange
        await _dbContext.Sequences.AddRangeAsync(
            new Sequence { Id = "seq1", Name = "Sequence 1", Description = "Description 1", WorstCaseTime = TimeSpan.Zero },
            new Sequence { Id = "seq2", Name = "Sequence 2", Description = "Description 2", WorstCaseTime = TimeSpan.Zero },
            new Sequence { Id = "seq3", Name = "Sequence 3", Description = "Description 3", WorstCaseTime = TimeSpan.Zero }
        );
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetSequencesByIdsAsync(new[] { "seq1", "seq3" });
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, s => s.Id == "seq1");
        Assert.Contains(result, s => s.Id == "seq3");
        Assert.DoesNotContain(result, s => s.Id == "seq2");
    }
}
