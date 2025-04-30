using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Repository;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Instrument.Scheduling.Data.UT;
public class UnitOfWorkTests
{
    private readonly Mock<SchedulerDbContext> _mockDbContext;
    private readonly UnitOfWork _unitOfWork;
    
    public UnitOfWorkTests()
    {
        // Create a mock DbContext
        _mockDbContext = new Mock<SchedulerDbContext>(new DbContextOptionsBuilder<SchedulerDbContext>().Options);
        
        // Set up DbContext mock
        _mockDbContext.Setup(db => db.SaveChangesAsync(default)).ReturnsAsync(1);
        
        // Create the UnitOfWork with the mock DbContext
        _unitOfWork = new UnitOfWork(_mockDbContext.Object);
    }
    
    [Fact]
    public void SequenceDefinitions_ReturnsRepositoryInstance()
    {
        // Act
        var repository = _unitOfWork.SequenceDefinitions;
        
        // Assert
        Assert.NotNull(repository);
        Assert.IsType<SequenceRepository>(repository);
    }
    
    [Fact]
    public void Parameters_ReturnsRepositoryInstance()
    {
        // Act
        var repository = _unitOfWork.Parameters;
        
        // Assert
        Assert.NotNull(repository);
        Assert.IsType<ParameterRepository>(repository);
    }
    
    [Fact]
    public void Ranges_ReturnsRepositoryInstance()
    {
        // Act
        var repository = _unitOfWork.Ranges;
        
        // Assert
        Assert.NotNull(repository);
        Assert.IsType<RangeRepository>(repository);
    }
    
    [Fact]
    public void RangeValues_ReturnsRepositoryInstance()
    {
        // Act
        var repository = _unitOfWork.RangeValues;
        
        // Assert
        Assert.NotNull(repository);
        Assert.IsType<RangeValueRepository>(repository);
    }
    
    [Fact]
    public void Resources_ReturnsRepositoryInstance()
    {
        // Act
        var repository = _unitOfWork.Resources;
        
        // Assert
        Assert.NotNull(repository);
        Assert.IsType<ResourceRepository>(repository);
    }
    
    [Fact]
    public void SequenceGroups_ReturnsRepositoryInstance()
    {
        // Act
        var repository = _unitOfWork.SequenceGroups;
        
        // Assert
        Assert.NotNull(repository);
        Assert.IsType<SequenceGroupRepository>(repository);
    }
    
    [Fact]
    public async Task SaveChangesAsync_CallsDbContextSaveChanges()
    {
        // Act
        var result = await _unitOfWork.SaveChangesAsync();
        
        // Assert
        Assert.Equal(1, result);
        _mockDbContext.Verify(db => db.SaveChangesAsync(default), Times.Once);
    }
    
    [Fact]
    public void Dispose_CallsDbContextDispose()
    {
        // Act
        _unitOfWork.Dispose();
        
        // Assert
        _mockDbContext.Verify(db => db.Dispose(), Times.Once);
    }
    
    [Fact]
    public void RepositoriesAreCached_ReturnsSameInstance()
    {
        // Act
        var repository1 = _unitOfWork.SequenceDefinitions;
        var repository2 = _unitOfWork.SequenceDefinitions;
        
        // Assert
        Assert.Same(repository1, repository2); // Should be the exact same instance, not just equal
    }
    
    [Fact]
    public void SequenceGroupRepositoryIsCached_ReturnsSameInstance()
    {
        // Act
        var repository1 = _unitOfWork.SequenceGroups;
        var repository2 = _unitOfWork.SequenceGroups;
        
        // Assert
        Assert.Same(repository1, repository2); // Should be the exact same instance, not just equal
    }
    
    [Fact]
    public void UnitOfWork_CreatesDistinctRepositories()
    {
        // Act
        var sequenceRepo = _unitOfWork.SequenceDefinitions;
        var parameterRepo = _unitOfWork.Parameters;
        var rangeRepo = _unitOfWork.Ranges;
        var rangeValueRepo = _unitOfWork.RangeValues;
        var resourceRepo = _unitOfWork.Resources;
        var sequenceGroupRepo = _unitOfWork.SequenceGroups;
        
        // Assert
        Assert.IsType<SequenceRepository>(sequenceRepo);
        Assert.IsType<ParameterRepository>(parameterRepo);
        Assert.IsType<RangeRepository>(rangeRepo);
        Assert.IsType<RangeValueRepository>(rangeValueRepo);
        Assert.IsType<ResourceRepository>(resourceRepo);
        Assert.IsType<SequenceGroupRepository>(sequenceGroupRepo);
        
        // Verify they are different instances
        Assert.NotSame(sequenceRepo, parameterRepo);
        Assert.NotSame(sequenceRepo, rangeRepo);
        Assert.NotSame(sequenceRepo, rangeValueRepo);
        Assert.NotSame(sequenceRepo, resourceRepo);
        Assert.NotSame(sequenceRepo, sequenceGroupRepo);
        Assert.NotSame(parameterRepo, rangeRepo);
        Assert.NotSame(parameterRepo, rangeValueRepo);
        Assert.NotSame(parameterRepo, resourceRepo);
        Assert.NotSame(parameterRepo, sequenceGroupRepo);
        Assert.NotSame(rangeRepo, rangeValueRepo);
        Assert.NotSame(rangeRepo, resourceRepo);
        Assert.NotSame(rangeRepo, sequenceGroupRepo);
        Assert.NotSame(rangeValueRepo, resourceRepo);
        Assert.NotSame(rangeValueRepo, sequenceGroupRepo);
        Assert.NotSame(resourceRepo, sequenceGroupRepo);
    }
    
    [Fact]
    public void Construction_WithNullDbContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UnitOfWork(null));
    }
}
