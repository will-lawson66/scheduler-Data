using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Instrument.Scheduling.Data.UT;

public class SequenceServiceTests
{
    private readonly Mock<ISequenceRepository> _mockSequenceRepository;
    private readonly Mock<ILogger<SequenceService>> _mockLogger;
    private readonly SequenceService _service;

    public SequenceServiceTests()
    {
        // Set up mocks
        _mockSequenceRepository = new Mock<ISequenceRepository>();
        _mockLogger = new Mock<ILogger<SequenceService>>();

        // Create the service
        _service = new SequenceService(
            _mockSequenceRepository.Object,
            _mockLogger.Object
        );
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

        _mockSequenceRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync(sequence);

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

        _mockSequenceRepository.Setup(repo => repo.GetByIdAsync(sequence.Id))
            .ReturnsAsync((Sequence?)null);

        // Act
        await _service.CreateSequenceAsync(sequence);

        // Assert
        _mockSequenceRepository.Verify(repo => repo.AddAsync(sequence), Times.Once);
    }

    [Fact]
    public async Task CreateSequenceAsync_WithExistingId_ThrowsSchedulerDataException()
    {
        // Arrange
        var id = "test-sequence-1";
        var sequence = new Sequence 
        { 
            Id = id, 
            Name = "Test Sequence", 
            WorstCaseTime = TimeSpan.FromSeconds(30) 
        };
        var existingSequence = new Sequence 
        { 
            Id = id, 
            Name = "Existing Sequence", 
            WorstCaseTime = TimeSpan.FromSeconds(45) 
        };

        _mockSequenceRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync(existingSequence);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SchedulerDataException>(() => 
            _service.CreateSequenceAsync(sequence));
            
        Assert.Contains(id, exception.Message);
        
        _mockSequenceRepository.Verify(repo => repo.AddAsync(It.IsAny<Sequence>()), Times.Never);
    }

    [Fact]
    public async Task UpdateSequenceAsync_WithValidSequence_UpdatesSequence()
    {
        // Arrange
        var id = "test-sequence-1";
        var sequence = new Sequence 
        { 
            Id = id, 
            Name = "Updated Sequence", 
            WorstCaseTime = TimeSpan.FromSeconds(30) 
        };
        var existingSequence = new Sequence 
        { 
            Id = id, 
            Name = "Original Sequence", 
            WorstCaseTime = TimeSpan.FromSeconds(45) 
        };

        _mockSequenceRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync(existingSequence);

        // Act
        await _service.UpdateSequenceAsync(sequence);

        // Assert
        _mockSequenceRepository.Verify(repo => repo.UpdateAsync(sequence), Times.Once);
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

        _mockSequenceRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Sequence?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.UpdateSequenceAsync(sequence));
            
        Assert.Equal(id, exception.EntityId);
        Assert.Equal("Sequence", exception.EntityType);
        
        _mockSequenceRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Sequence>()), Times.Never);
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

        _mockSequenceRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync(existingSequence);

        // Act
        await _service.DeleteSequenceAsync(id);

        // Assert
        _mockSequenceRepository.Verify(repo => repo.DeleteAsync(id), Times.Once);
    }

    [Fact]
    public async Task DeleteSequenceAsync_WithNonExistingId_ThrowsEntityNotFoundException()
    {
        // Arrange
        var id = "test-sequence-1";

        _mockSequenceRepository.Setup(repo => repo.GetByIdAsync(id))
            .ReturnsAsync((Sequence?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.DeleteSequenceAsync(id));
            
        Assert.Equal(id, exception.EntityId);
        Assert.Equal("Sequence", exception.EntityType);
        
        _mockSequenceRepository.Verify(repo => repo.DeleteAsync(id), Times.Never);
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

        _mockSequenceRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(sequences);

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

        _mockSequenceRepository.Setup(repo => repo.GetQueryableAsync())
            .ReturnsAsync(sequences.AsQueryable());

        // Act
        var result = await _service.SearchSequencesAsync(s => s.Name.Contains("Alpha"));

        // Assert
        Assert.Single(result);
        Assert.Equal("seq1", result.First().Id);
    }
    
    [Fact]
    public async Task GetAllSequencesAsync_WhenRepositoryThrowsException_ThrowsStorageProviderException()
    {
        // Arrange
        _mockSequenceRepository.Setup(repo => repo.GetAllAsync())
            .ThrowsAsync(new Exception("Test error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<StorageProviderException>(() => 
            _service.GetAllSequencesAsync());
            
        Assert.Equal("GetAllSequences", exception.Operation);
        Assert.NotNull(exception.InnerException);
    }
    
    [Fact]
    public async Task SearchSequencesAsync_WhenRepositoryThrowsException_ThrowsStorageProviderException()
    {
        // Arrange
        _mockSequenceRepository.Setup(repo => repo.GetQueryableAsync())
            .ThrowsAsync(new Exception("Test error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<StorageProviderException>(() => 
            _service.SearchSequencesAsync(s => true));
            
        Assert.Equal("SearchSequences", exception.Operation);
        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<SequenceService>>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SequenceService(null!, logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        ILogger<SequenceService>? nullLogger = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SequenceService(_mockSequenceRepository.Object, null!));
    }
}
