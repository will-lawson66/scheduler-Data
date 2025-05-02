using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Instrument.Data.Exceptions;
using Instrument.Data.Interfaces;
using Instrument.Data.Repository;
using Instrument.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Instrument.Data.UT;

public class SequenceServiceTests : IDisposable
{
    private readonly SchedulerDbContext _dbContext;
    private readonly ISequenceRepository _sequenceRepository;
    private readonly Mock<ILogger<SequenceService>> _mockLogger;
    private readonly SequenceService _service;

    public SequenceServiceTests()
    {
        // Set up in-memory database
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new SchedulerDbContext(options);

        // Set up real repository with in-memory database
        _sequenceRepository = new SequenceRepository(_dbContext);
        
        // Set up logger mock
        _mockLogger = new Mock<ILogger<SequenceService>>();

        // Create the service
        _service = new SequenceService(
            _sequenceRepository,
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
    public async Task GetSequenceAsync_WithValidId_ReturnsSequence()
    {
        // Arrange
        var id = "test-sequence-1";
        var sequence = new Sequence 
        { 
            Id = id, 
            Name = "Test Sequence", 
            WorstCaseTime = TimeSpan.FromSeconds(30) 
        };

        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetSequenceAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Test Sequence", result.Name);
        Assert.Equal(TimeSpan.FromSeconds(30), result.WorstCaseTime);
    }

    [Fact]
    public async Task CreateSequenceAsync_WithValidSequence_CreatesSequence()
    {
        // Arrange
        var sequence = new Sequence 
        { 
            Id = "test-sequence-1", 
            Name = "Test Sequence", 
            WorstCaseTime = TimeSpan.FromSeconds(30) 
        };

        // Act
        await _service.CreateSequenceAsync(sequence);

        // Assert
        var createdSequence = await _dbContext.Sequences.FindAsync(sequence.Id);
        Assert.NotNull(createdSequence);
        Assert.Equal("Test Sequence", createdSequence.Name);
        Assert.Equal(TimeSpan.FromSeconds(30), createdSequence.WorstCaseTime);
    }

    [Fact]
    public async Task CreateSequenceAsync_WithExistingId_ThrowsSchedulerDataException()
    {
        // Arrange
        var id = "test-sequence-1";
        var existingSequence = new Sequence 
        { 
            Id = id, 
            Name = "Existing Sequence", 
            WorstCaseTime = TimeSpan.FromSeconds(45) 
        };
        
        await _dbContext.Sequences.AddAsync(existingSequence);
        await _dbContext.SaveChangesAsync();

        var newSequence = new Sequence 
        { 
            Id = id, 
            Name = "Test Sequence", 
            WorstCaseTime = TimeSpan.FromSeconds(30) 
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SchedulerDataException>(() => 
            _service.CreateSequenceAsync(newSequence));
            
        Assert.Contains(id, exception.Message);
        
        // Verify the sequence wasn't changed
        var unchangedSequence = await _dbContext.Sequences.FindAsync(id);
        Assert.Equal("Existing Sequence", unchangedSequence.Name);
    }

    [Fact]
    public async Task UpdateSequenceAsync_WithValidSequence_UpdatesSequence()
    {
        // Arrange
        var id = "test-sequence-1";
        var existingSequence = new Sequence 
        { 
            Id = id, 
            Name = "Original Sequence", 
            WorstCaseTime = TimeSpan.FromSeconds(45) 
        };
        
        await _dbContext.Sequences.AddAsync(existingSequence);
        await _dbContext.SaveChangesAsync();

        var updatedSequence = new Sequence 
        { 
            Id = id, 
            Name = "Updated Sequence", 
            WorstCaseTime = TimeSpan.FromSeconds(30) 
        };

        // Act
        await _service.UpdateSequenceAsync(updatedSequence);

        // Assert
        var resultSequence = await _dbContext.Sequences.FindAsync(id);
        Assert.NotNull(resultSequence);
        Assert.Equal("Updated Sequence", resultSequence.Name);
        Assert.Equal(TimeSpan.FromSeconds(30), resultSequence.WorstCaseTime);
    }

    [Fact]
    public async Task UpdateSequenceAsync_WithNonExistingId_ThrowsEntityNotFoundException()
    {
        // Arrange
        var id = "test-sequence-1";
        var sequence = new Sequence 
        { 
            Id = id, 
            Name = "Test Sequence", 
            WorstCaseTime = TimeSpan.FromSeconds(30) 
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.UpdateSequenceAsync(sequence));
            
        Assert.Equal(id, exception.EntityId);
        Assert.Equal("Sequence", exception.EntityType);
        
        // Verify no sequence was added
        var sequenceCount = await _dbContext.Sequences.CountAsync();
        Assert.Equal(0, sequenceCount);
    }
    
    [Fact]
    public async Task UpdateSequencePropertiesAsync_WithValidId_UpdatesSpecifiedProperties()
    {
        // Arrange
        var id = "test-sequence-1";
        var existingSequence = new Sequence 
        { 
            Id = id, 
            Name = "Original Sequence", 
            WorstCaseTime = TimeSpan.FromSeconds(45),
            Description = "Original description",
            CanBeParallel = false
        };
        
        await _dbContext.Sequences.AddAsync(existingSequence);
        await _dbContext.SaveChangesAsync();

        // Act - only update name and description
        var result = await _service.UpdateSequencePropertiesAsync(
            id: id,
            name: "Updated Sequence",
            description: "Updated description"
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Sequence", result.Name);
        Assert.Equal("Updated description", result.Description);
        Assert.Equal(TimeSpan.FromSeconds(45), result.WorstCaseTime); // Unchanged
        Assert.False(result.CanBeParallel); // Unchanged
        
        // Verify database changes
        _dbContext.ChangeTracker.Clear(); // Clear tracking to ensure we get fresh data
        var updatedSequence = await _dbContext.Sequences.FindAsync(id);
        Assert.Equal("Updated Sequence", updatedSequence.Name);
        Assert.Equal("Updated description", updatedSequence.Description);
        Assert.Equal(TimeSpan.FromSeconds(45), updatedSequence.WorstCaseTime);
        Assert.False(updatedSequence.CanBeParallel);
    }
    
    [Fact]
    public async Task UpdateSequencePropertiesAsync_WithAllProperties_UpdatesAllProperties()
    {
        // Arrange
        var id = "test-sequence-1";
        var existingSequence = new Sequence 
        { 
            Id = id, 
            Name = "Original Sequence", 
            WorstCaseTime = TimeSpan.FromSeconds(45),
            Description = "Original description",
            CanBeParallel = false
        };
        
        await _dbContext.Sequences.AddAsync(existingSequence);
        await _dbContext.SaveChangesAsync();

        // Act - update all properties
        var result = await _service.UpdateSequencePropertiesAsync(
            id: id,
            name: "Updated Sequence",
            worstCaseTime: TimeSpan.FromSeconds(30),
            description: "Updated description",
            canBeParallel: true
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Sequence", result.Name);
        Assert.Equal("Updated description", result.Description);
        Assert.Equal(TimeSpan.FromSeconds(30), result.WorstCaseTime);
        Assert.True(result.CanBeParallel);
        
        // Verify database changes
        _dbContext.ChangeTracker.Clear();
        var updatedSequence = await _dbContext.Sequences.FindAsync(id);
        Assert.Equal("Updated Sequence", updatedSequence.Name);
        Assert.Equal("Updated description", updatedSequence.Description);
        Assert.Equal(TimeSpan.FromSeconds(30), updatedSequence.WorstCaseTime);
        Assert.True(updatedSequence.CanBeParallel);
    }
    
    [Fact]
    public async Task UpdateSequencePropertiesAsync_WithNonExistingId_ThrowsEntityNotFoundException()
    {
        // Arrange
        var id = "test-sequence-1";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.UpdateSequencePropertiesAsync(
                id: id,
                name: "Updated Sequence"
            ));
            
        Assert.Equal(id, exception.EntityId);
        Assert.Equal("Sequence", exception.EntityType);
    }

    [Fact]
    public async Task DeleteSequenceAsync_WithValidId_DeletesSequence()
    {
        // Arrange
        var id = "test-sequence-1";
        var existingSequence = new Sequence 
        { 
            Id = id, 
            Name = "Sequence to Delete", 
            WorstCaseTime = TimeSpan.FromSeconds(30) 
        };
        
        await _dbContext.Sequences.AddAsync(existingSequence);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.DeleteSequenceAsync(id);

        // Assert
        var deletedSequence = await _dbContext.Sequences.FindAsync(id);
        Assert.Null(deletedSequence);
    }

    [Fact]
    public async Task DeleteSequenceAsync_WithNonExistingId_ThrowsEntityNotFoundException()
    {
        // Arrange
        var id = "test-sequence-1";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.DeleteSequenceAsync(id));
            
        Assert.Equal(id, exception.EntityId);
        Assert.Equal("Sequence", exception.EntityType);
    }

    [Fact]
    public async Task GetAllSequencesAsync_ReturnsAllSequences()
    {
        // Arrange
        var sequences = new List<Sequence>
        {
            new Sequence { Id = "seq1", Name = "Sequence 1", WorstCaseTime = TimeSpan.FromSeconds(30) },
            new Sequence { Id = "seq2", Name = "Sequence 2", WorstCaseTime = TimeSpan.FromSeconds(45) }
        };

        await _dbContext.Sequences.AddRangeAsync(sequences);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetAllSequencesAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, s => s.Id == "seq1");
        Assert.Contains(result, s => s.Id == "seq2");
    }

    [Fact]
    public async Task SearchSequencesAsync_WithPredicate_ReturnsFilteredSequences()
    {
        // Arrange
        var sequences = new List<Sequence>
        {
            new Sequence { Id = "seq1", Name = "Alpha Sequence", WorstCaseTime = TimeSpan.FromSeconds(30) },
            new Sequence { Id = "seq2", Name = "Beta Sequence", WorstCaseTime = TimeSpan.FromSeconds(45) }
        };

        await _dbContext.Sequences.AddRangeAsync(sequences);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.SearchSequencesAsync(s => s.Name.Contains("Alpha"));

        // Assert
        Assert.Single(result);
        Assert.Equal("seq1", result.First().Id);
    }
    
    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<SequenceService>>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SequenceService(null!, logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SequenceService(_sequenceRepository, null!));
    }
}
