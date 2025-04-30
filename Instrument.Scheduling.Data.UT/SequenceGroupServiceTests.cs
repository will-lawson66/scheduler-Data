using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Instrument.Scheduling.Data.UT;

public class SequenceGroupServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ISequenceGroupRepository> _mockSequenceGroupRepository;
    private readonly Mock<ISequenceRepository> _mockSequenceRepository;
    private readonly Mock<ILogger<SequenceGroupService>> _mockLogger;
    private readonly SchedulerDbContext _dbContext;
    private readonly SequenceGroupService _service;

    public SequenceGroupServiceTests()
    {
        // Set up mocks
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockSequenceGroupRepository = new Mock<ISequenceGroupRepository>();
        _mockSequenceRepository = new Mock<ISequenceRepository>();
        _mockLogger = new Mock<ILogger<SequenceGroupService>>();

        // Set up DbContext with in-memory database
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new SchedulerDbContext(options);

        // Configure UnitOfWork mock to return our repository mocks
        _mockUnitOfWork.Setup(uow => uow.SequenceGroups).Returns(_mockSequenceGroupRepository.Object);
        _mockUnitOfWork.Setup(uow => uow.SequenceDefinitions).Returns(_mockSequenceRepository.Object);

        // Create the service
        _service = new SequenceGroupService(
            _mockUnitOfWork.Object,
            _dbContext,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CreateSequenceGroupAsync_WithValidData_CreatesGroup()
    {
        // Arrange
        var id = "test-group-1";
        var name = "Test Group";
        var description = "Test description";

        _mockSequenceGroupRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((SequenceGroup?)null);

        // Act
        var result = await _service.CreateSequenceGroupAsync(id, name, description);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal(name, result.Name);
        Assert.Equal(description, result.Description);

        _mockSequenceGroupRepository.Verify(repo => repo.AddAsync(It.IsAny<SequenceGroup>()), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateSequenceGroupAsync_WithExistingId_ThrowsSchedulerDataException()
    {
        // Arrange
        var id = "test-group-1";
        var name = "Test Group";
        var existingGroup = new SequenceGroup { Id = id, Name = "Existing Group" };

        _mockSequenceGroupRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync(existingGroup);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SchedulerDataException>(() => 
            _service.CreateSequenceGroupAsync(id, name));

        Assert.Contains(id, exception.Message);
        
        _mockSequenceGroupRepository.Verify(repo => repo.AddAsync(It.IsAny<SequenceGroup>()), Times.Never);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAllSequenceGroupsAsync_ReturnsAllGroups()
    {
        // Arrange
        var groups = new List<SequenceGroup>
        {
            new SequenceGroup { Id = "group1", Name = "Group 1" },
            new SequenceGroup { Id = "group2", Name = "Group 2" }
        };

        _mockSequenceGroupRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(groups);

        // Act
        var result = await _service.GetAllSequenceGroupsAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, g => g.Id == "group1");
        Assert.Contains(result, g => g.Id == "group2");
    }

    [Fact]
    public async Task DeleteSequenceGroupAsync_WithValidId_DeletesGroup()
    {
        // Arrange
        var id = "test-group-1";
        var existingGroup = new SequenceGroup { Id = id, Name = "Group To Delete" };

        _mockSequenceGroupRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync(existingGroup);

        // Act
        var result = await _service.DeleteSequenceGroupAsync(id);

        // Assert
        Assert.True(result);
        _mockSequenceGroupRepository.Verify(repo => repo.DeleteAsync(id), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteSequenceGroupAsync_WithNonExistingId_ThrowsEntityNotFoundException()
    {
        // Arrange
        var id = "test-group-1";

        _mockSequenceGroupRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((SequenceGroup?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.DeleteSequenceGroupAsync(id));
            
        Assert.Equal(id, exception.EntityId);
        Assert.Equal("SequenceGroup", exception.EntityType);
        
        _mockSequenceGroupRepository.Verify(repo => repo.DeleteAsync(id), Times.Never);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetSequenceGroupByIdAsync_WithValidId_ReturnsGroup()
    {
        // Arrange
        var id = "test-group-1";
        var group = new SequenceGroup { Id = id, Name = "Test Group" };

        _mockSequenceGroupRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync(group);

        // Act
        var result = await _service.GetSequenceGroupByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Test Group", result.Name);
    }

    [Fact]
    public async Task GetSequenceGroupWithSequencesAsync_WithValidId_ReturnsGroupWithSequences()
    {
        // Arrange
        var id = "test-group-1";
        var group = new SequenceGroup 
        { 
            Id = id, 
            Name = "Test Group",
            SequenceGroupSequences = new List<SequenceGroupSequences>
            {
                new SequenceGroupSequences 
                { 
                    SequenceId = "seq1", 
                    SequenceGroupId = id, 
                    Order = 1,
                    Sequence = new Sequence { Id = "seq1", Name = "Sequence 1", WorstCaseTime  = TimeSpan.FromMilliseconds(30000)} 
                }
            }
        };

        _mockSequenceGroupRepository.Setup(repo => repo.GetWithSequencesAsync(id))
            .ReturnsAsync(group);

        // Act
        var result = await _service.GetSequenceGroupWithSequencesAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Single(result.SequenceGroupSequences);
        Assert.Equal("seq1", result.SequenceGroupSequences[0].SequenceId);
    }
    
    [Fact]
    public async Task AddSequenceToGroupAsync_WithValidData_AddsSequence()
    {
        // Arrange
        var groupId = "test-group-1";
        var sequenceId = "seq1";
        var order = 1;
        
        var group = new SequenceGroup { Id = groupId, Name = "Test Group" };
        var sequence = new Sequence { Id = sequenceId, Name = "Test Sequence", WorstCaseTime = TimeSpan.FromMilliseconds(30000)};
        
        _mockSequenceGroupRepository.Setup(repo => repo.GetByIdAsync(groupId))
            .ReturnsAsync(group);
            
        _mockSequenceRepository.Setup(repo => repo.GetByIdAsync(sequenceId))
            .ReturnsAsync(sequence);
            
        // Act
        var result = await _service.AddSequenceToGroupAsync(groupId, sequenceId, order);
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public async Task AddSequenceToGroupAsync_WithInvalidGroupId_ThrowsEntityNotFoundException()
    {
        // Arrange
        var groupId = "invalid-group";
        var sequenceId = "seq1";
        var order = 1;
        
        _mockSequenceGroupRepository.Setup(repo => repo.GetByIdAsync(groupId))
            .ReturnsAsync((SequenceGroup?)null);
            
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.AddSequenceToGroupAsync(groupId, sequenceId, order));
            
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
        
        _mockSequenceGroupRepository.Setup(repo => repo.GetByIdAsync(groupId))
            .ReturnsAsync(group);
            
        _mockSequenceRepository.Setup(repo => repo.GetByIdAsync(sequenceId))
            .ReturnsAsync((Sequence?)null);
            
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.AddSequenceToGroupAsync(groupId, sequenceId, order));
            
        Assert.Equal(sequenceId, exception.EntityId);
        Assert.Equal("Sequence", exception.EntityType);
    }
    
    [Fact]
    public async Task ValidateSequenceGroupAsync_WithNonExistingGroupId_ThrowsEntityNotFoundException()
    {
        // Arrange
        var groupId = "invalid-group";
        
        _mockSequenceGroupRepository.Setup(repo => repo.GetWithSequencesAsync(groupId))
            .ReturnsAsync((SequenceGroup?)null);
            
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.ValidateSequenceGroupAsync(groupId));
            
        Assert.Equal(groupId, exception.EntityId);
        Assert.Equal("SequenceGroup", exception.EntityType);
    }
}
