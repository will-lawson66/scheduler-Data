using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Instrument.Data.Exceptions;
using Instrument.Data.Repository;
using Instrument.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Instrument.Data.UT;

public class SequenceGroupServiceTests : IDisposable
{
    private readonly SchedulerDbContext _dbContext;
    private readonly ISequenceGroupRepository _sequenceGroupRepository;
    private readonly ISequenceRepository _sequenceRepository;
    private readonly Mock<ILogger<SequenceGroupService>> _mockLogger;
    private readonly SequenceGroupService _service;

    public SequenceGroupServiceTests()
    {
        // Set up in-memory database
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new SchedulerDbContext(options);

        // Set up real repositories with in-memory database
        _sequenceGroupRepository = new SequenceGroupRepository(_dbContext);
        _sequenceRepository = new SequenceRepository(_dbContext);
        
        // Set up logger mock
        _mockLogger = new Mock<ILogger<SequenceGroupService>>();

        // Create the service
        _service = new SequenceGroupService(
            _sequenceGroupRepository,
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
    public async Task CreateSequenceGroupAsync_WithValidData_CreatesGroup()
    {
        // Arrange
        var sequenceGroup = new SequenceGroup
        {
            Name = "Test Group",
            Description = "Test description",
        };
 

        // Act
        var result = await _service.CreateSequenceGroupAsync(sequenceGroup);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sequenceGroup.Name, result.Name);
        Assert.Equal(sequenceGroup.Description, result.Description);

        var savedGroup = await _dbContext.SequenceGroups.FindAsync(sequenceGroup.Id);
        Assert.NotNull(savedGroup);
        Assert.Equal(sequenceGroup.Name, savedGroup.Name);
        Assert.Equal(sequenceGroup.Description, savedGroup.Description);
    }

    [Fact]
    public async Task GetAllSequenceGroupsAsync_ReturnsAllGroups()
    {
        // Arrange
        var groups = new List<SequenceGroup>
        {
            new()
                { Name = "Group 1" },
            new()
                { Name = "Group 2" }
        };

        await _dbContext.SequenceGroups.AddRangeAsync(groups);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetAllSequenceGroupsAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, g => g?.Name == "Group 1");
        Assert.Contains(result, g => g?.Name == "Group 2");
    }

    [Fact]
    public async Task DeleteSequenceGroupAsync_WithValidId_DeletesGroup()
    {
        // Arrange
        var existingGroup = new SequenceGroup { Name = "Group To Delete" };

        await _dbContext.SequenceGroups.AddAsync(existingGroup);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.DeleteSequenceGroupAsync(existingGroup.Id);

        // Assert
        var deletedGroup = await _dbContext.SequenceGroups.FindAsync(existingGroup.Id);
        Assert.Null(deletedGroup);
    }

    [Fact]
    public async Task DeleteSequenceGroupAsync_WithNonExistingId_ThrowsEntityNotFoundException()
    {
        // Arrange
        var id =-4;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.DeleteSequenceGroupAsync(id));
            
        Assert.Equal(id, exception.EntityId);
        Assert.Equal("SequenceGroup", exception.EntityType);
    }

    [Fact]
    public async Task GetSequenceGroupByIdAsync_WithValidId_ReturnsGroup()
    {
        // Arrange
        var group = new SequenceGroup { Name = "Test Group" };

        await _dbContext.SequenceGroups.AddAsync(group);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetSequenceGroupByIdAsync(group.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(group.Name, result.Name);
        Assert.Equal("Test Group", result.Name);
    }

    [Fact]
    public async Task AddSequenceToGroupAsync_WithValidData_AddsSequence()
    {
        // Arrange
        var group = new SequenceGroup { Name = "Test Group" };
        var sequence = new Sequence { 
            Name = "Test Sequence", 
            WorstCaseTime = TimeSpan.FromMilliseconds(30000)
        };
        
        await _dbContext.SequenceGroups.AddAsync(group);
        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();
            
        // Act
        var result = await _service.AddSequenceToSequenceGroupAsync(group.Id, sequence.Id);
        
        // Make sure all changes are committed before any assertions
        await _dbContext.SaveChangesAsync();

        // Detach all entities to ensure fresh retrieval
        _dbContext.ChangeTracker.Clear();
        
        // Assert
        Assert.True(result);
        
        // Verify association was created using AsNoTracking to get fresh data
        var association = await _dbContext.SequenceGroupSequences
            .AsNoTracking()
            .FirstOrDefaultAsync(sgs => sgs.SequenceGroupId == group.Id && sgs.SequenceId == sequence.Id);
        Assert.NotNull(association);
    }
    
    [Fact]
    public async Task AddSequenceToGroupAsync_WithInvalidSequenceId_ThrowsEntityNotFoundException()
    {
        // Arrange
        var group = new SequenceGroup { Name = "Test Group" };
        var sequenceId = -2;
        
        await _dbContext.SequenceGroups.AddAsync(group);
        await _dbContext.SaveChangesAsync();
            
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.AddSequenceToSequenceGroupAsync(group.Id, sequenceId));
            
        Assert.Equal(sequenceId, exception.EntityId);
        Assert.Equal("Sequence", exception.EntityType);
    }
}
