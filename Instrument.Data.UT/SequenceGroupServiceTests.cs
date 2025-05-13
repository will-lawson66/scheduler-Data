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
        var id = "test-group-1";
        var name = "Test Group";
        var description = "Test description";

        // Act
        var result = await _service.CreateSequenceGroupAsync(id, name, description);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal(name, result.Name);
        Assert.Equal(description, result.Description);

        var savedGroup = await _dbContext.SequenceGroups.FindAsync(id);
        Assert.NotNull(savedGroup);
        Assert.Equal(name, savedGroup.Name);
        Assert.Equal(description, savedGroup.Description);
    }

    [Fact]
    public async Task CreateSequenceGroupAsync_WithExistingId_ThrowsSchedulerDataException()
    {
        // Arrange
        var id = "test-group-1";
        var name = "Test Group";
        var existingGroup = new SequenceGroup { Id = id, Name = "Existing Group" };

        await _dbContext.SequenceGroups.AddAsync(existingGroup);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SchedulerDataException>(() => 
            _service.CreateSequenceGroupAsync(id, name));

        Assert.Contains(id, exception.Message);
        
        // Verify the group wasn't changed
        var unchangedGroup = await _dbContext.SequenceGroups.FindAsync(id);
        Assert.Equal("Existing Group", unchangedGroup?.Name);
    }

    [Fact]
    public async Task GetAllSequenceGroupsAsync_ReturnsAllGroups()
    {
        // Arrange
        var groups = new List<SequenceGroup>
        {
            new()
                { Id = "group1", Name = "Group 1" },
            new()
                { Id = "group2", Name = "Group 2" }
        };

        await _dbContext.SequenceGroups.AddRangeAsync(groups);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetAllSequenceGroupsAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, g => g?.Id == "group1");
        Assert.Contains(result, g => g?.Id == "group2");
    }

    [Fact]
    public async Task DeleteSequenceGroupAsync_WithValidId_DeletesGroup()
    {
        // Arrange
        var id = "test-group-1";
        var existingGroup = new SequenceGroup { Id = id, Name = "Group To Delete" };

        await _dbContext.SequenceGroups.AddAsync(existingGroup);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.DeleteSequenceGroupAsync(id);

        // Assert
        var deletedGroup = await _dbContext.SequenceGroups.FindAsync(id);
        Assert.Null(deletedGroup);
    }

    [Fact]
    public async Task DeleteSequenceGroupAsync_WithNonExistingId_ThrowsEntityNotFoundException()
    {
        // Arrange
        var id = "test-group-1";

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
        var id = "test-group-1";
        var group = new SequenceGroup { Id = id, Name = "Test Group" };

        await _dbContext.SequenceGroups.AddAsync(group);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetSequenceGroupByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Test Group", result.Name);
    }

    [Fact]
    public async Task GetSequenceGroupByIdAsync_ReturnsGroup_WithSequences()
    {
        // Arrange
        var id = "test-group-1";
        var group = new SequenceGroup { Id = id, Name = "Test Group" };

        await _dbContext.SequenceGroups.AddAsync(group);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetSequenceGroupByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Test Group", result.Name);
    }

    [Fact]
    public async Task GetSequenceGroupByIdAsync_WithValidId_ReturnsGroupWithSequences()
    {
        // Arrange
        var groupId = "test-group-1";
        var sequenceId = "test-sequence-1";
        var sequence = new Sequence { 
            Id = sequenceId, 
            Name = "Sequence 1", 
            WorstCaseTime = TimeSpan.FromMilliseconds(30000)
        };
        
        var group = new SequenceGroup { 
            Id = groupId, 
            Name = "Test Group"
        };

        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SequenceGroups.AddAsync(group);
        await _dbContext.SaveChangesAsync();

        // Create association
        var sequenceGroupSequence = new SequenceGroupSequence {
            SequenceId = sequenceId,
            SequenceGroupId = groupId,
            Order = 1
        };

        await _dbContext.SequenceGroupSequences.AddAsync(sequenceGroupSequence);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetSequenceGroupByIdAsync(groupId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(groupId, result.Id);
        Assert.NotNull(result.SequenceGroupSequences);
        Assert.Single(result.SequenceGroupSequences);
        Assert.Equal(sequenceId, result.SequenceGroupSequences.First().SequenceId);
    }
    
    [Fact]
    public async Task AddSequenceToGroupAsync_WithValidData_AddsSequence()
    {
        // Arrange
        var groupId = "test-group-1";
        var sequenceId = "seq1";
        var order = 1;
        
        var group = new SequenceGroup { Id = groupId, Name = "Test Group" };
        var sequence = new Sequence { 
            Id = sequenceId, 
            Name = "Test Sequence", 
            WorstCaseTime = TimeSpan.FromMilliseconds(30000)
        };
        
        await _dbContext.SequenceGroups.AddAsync(group);
        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();
            
        // Act
        var result = await _service.AddSequenceToSequenceGroupAsync(groupId, sequenceId, order);
        
        // Make sure all changes are committed before any assertions
        await _dbContext.SaveChangesAsync();

        // Detach all entities to ensure fresh retrieval
        _dbContext.ChangeTracker.Clear();
        
        // Assert
        Assert.True(result);
        
        // Verify association was created using AsNoTracking to get fresh data
        var association = await _dbContext.SequenceGroupSequences
            .AsNoTracking()
            .FirstOrDefaultAsync(sgs => sgs.SequenceGroupId == groupId && sgs.SequenceId == sequenceId);
        Assert.NotNull(association);
        Assert.Equal(order, association.Order);
    }
    
    [Fact]
    public async Task AddSequenceToGroupAsync_WithInvalidGroupId_ThrowsEntityNotFoundException()
    {
        // Arrange
        var groupId = "invalid-group";
        var sequenceId = "seq1";
        var order = 1;
        
        var sequence = new Sequence { 
            Id = sequenceId, 
            Name = "Test Sequence", 
            WorstCaseTime = TimeSpan.FromMilliseconds(30000)
        };
        
        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();
            
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.AddSequenceToSequenceGroupAsync(groupId, sequenceId, order));
            
        Assert.Equal(groupId, exception.EntityId);
        Assert.Equal("SequenceGroup", exception.EntityType);
    }
    
    [Fact]
    public async Task AddSequenceToGroupAsync_WithInvalidSequenceId_ThrowsEntityNotFoundException()
    {
        // Arrange
        var groupId = "test-group-1";
        var sequenceId = "invalid-seq";
        var order = 1;
        
        var group = new SequenceGroup { Id = groupId, Name = "Test Group" };
        
        await _dbContext.SequenceGroups.AddAsync(group);
        await _dbContext.SaveChangesAsync();
            
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.AddSequenceToSequenceGroupAsync(groupId, sequenceId, order));
            
        Assert.Equal(sequenceId, exception.EntityId);
        Assert.Equal("Sequence", exception.EntityType);
    }
    
    [Fact]
    public async Task ValidateSequenceGroupAsync_WithNonExistingGroupId_ThrowsEntityNotFoundException()
    {
        // Arrange
        var groupId = "invalid-group";
            
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.ValidateSequenceGroupAsync(groupId));
            
        Assert.Equal(groupId, exception.EntityId);
        Assert.Equal("SequenceGroup", exception.EntityType);
    }
}
