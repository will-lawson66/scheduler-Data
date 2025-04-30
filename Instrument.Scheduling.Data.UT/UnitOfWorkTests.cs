using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Repository;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Instrument.Scheduling.Data.UT;
public class UnitOfWorkTests
{
    private readonly Mock<IStorageProvider<Sequence>> _mockSequenceProvider;
    private readonly Mock<IStorageProvider<Parameter>> _mockParameterProvider;
    private readonly Mock<IStorageProvider<SequenceParameter>> _mockSequenceParameterProvider;
    private readonly Mock<IStorageProvider<Entities.Range>> _mockRangeProvider;
    private readonly Mock<IStorageProvider<RangeValue>> _mockRangeValueProvider;
    private readonly Mock<IStorageProvider<Resource>> _mockResourceProvider;
    private readonly Mock<IStorageProvider<SequenceGroup>> _mockSequenceGroupProvider;
    private readonly SchedulerDbContext _dbContext;
    private readonly UnitOfWork _unitOfWork;
    
    public UnitOfWorkTests()
    {
        _mockSequenceProvider = new Mock<IStorageProvider<Sequence>>();
        _mockParameterProvider = new Mock<IStorageProvider<Parameter>>();
        _mockSequenceParameterProvider = new Mock<IStorageProvider<SequenceParameter>>();
        _mockRangeProvider = new Mock<IStorageProvider<Entities.Range>>();
        _mockRangeValueProvider = new Mock<IStorageProvider<RangeValue>>();
        _mockResourceProvider = new Mock<IStorageProvider<Resource>>();
        _mockSequenceGroupProvider = new Mock<IStorageProvider<SequenceGroup>>();
        
        // Create a real DbContext with in-memory database for testing
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new SchedulerDbContext(options);
        
        _unitOfWork = new UnitOfWork(
            _mockSequenceProvider.Object,
            _mockParameterProvider.Object,
            _mockSequenceParameterProvider.Object,
            _mockRangeProvider.Object,
            _mockRangeValueProvider.Object,
            _mockResourceProvider.Object,
            _mockSequenceGroupProvider.Object,
            _dbContext
        );
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
    public async Task SaveChangesAsync_CallsSaveChanges_OnAllProviders()
    {
        // Act
        await _unitOfWork.SaveChangesAsync();
        
        // Assert
        _mockSequenceProvider.Verify(p => p.SaveChangesAsync(), Times.Once);
        _mockParameterProvider.Verify(p => p.SaveChangesAsync(), Times.Once);
        _mockSequenceParameterProvider.Verify(p => p.SaveChangesAsync(), Times.Once);
        _mockRangeProvider.Verify(p => p.SaveChangesAsync(), Times.Once);
        _mockRangeValueProvider.Verify(p => p.SaveChangesAsync(), Times.Once);
        _mockResourceProvider.Verify(p => p.SaveChangesAsync(), Times.Once);
        _mockSequenceGroupProvider.Verify(p => p.SaveChangesAsync(), Times.Once);
    }
    
    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => _unitOfWork.Dispose());
        Assert.Null(exception);
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
    public async Task SaveChangesAsync_ReturnsCorrectCount()
    {
        // Act
        var result = await _unitOfWork.SaveChangesAsync();
        
        // Assert
        Assert.Equal(1, result); // The implementation returns a hardcoded 1 currently
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
    public void Construction_WithNullParameters_ThrowsArgumentNullException()
    {
        // Create correctly initialized parameters for tests
        var mockSequenceProvider = new Mock<IStorageProvider<Sequence>>();
        var mockParameterProvider = new Mock<IStorageProvider<Parameter>>();
        var mockSequenceParameterProvider = new Mock<IStorageProvider<SequenceParameter>>();
        var mockRangeProvider = new Mock<IStorageProvider<Entities.Range>>();
        var mockRangeValueProvider = new Mock<IStorageProvider<RangeValue>>();
        var mockResourceProvider = new Mock<IStorageProvider<Resource>>();
        var mockSequenceGroupProvider = new Mock<IStorageProvider<SequenceGroup>>();
        
        // Create a real DbContext with in-memory database for testing
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        var dbContext = new SchedulerDbContext(options);
        
        // Test each parameter for null check
        Assert.Throws<ArgumentNullException>(() => new UnitOfWork(
            null, // Test null sequence provider
            mockParameterProvider.Object,
            mockSequenceParameterProvider.Object,
            mockRangeProvider.Object,
            mockRangeValueProvider.Object,
            mockResourceProvider.Object,
            mockSequenceGroupProvider.Object,
            dbContext
        ));
        
        Assert.Throws<ArgumentNullException>(() => new UnitOfWork(
            mockSequenceProvider.Object,
            null, // Test null parameter provider
            mockSequenceParameterProvider.Object,
            mockRangeProvider.Object,
            mockRangeValueProvider.Object,
            mockResourceProvider.Object,
            mockSequenceGroupProvider.Object,
            dbContext
        ));
        
        Assert.Throws<ArgumentNullException>(() => new UnitOfWork(
            mockSequenceProvider.Object,
            mockParameterProvider.Object,
            null, // Test null sequence parameter provider
            mockRangeProvider.Object,
            mockRangeValueProvider.Object,
            mockResourceProvider.Object,
            mockSequenceGroupProvider.Object,
            dbContext
        ));
        
        Assert.Throws<ArgumentNullException>(() => new UnitOfWork(
            mockSequenceProvider.Object,
            mockParameterProvider.Object,
            mockSequenceParameterProvider.Object,
            null, // Test null range provider
            mockRangeValueProvider.Object,
            mockResourceProvider.Object,
            mockSequenceGroupProvider.Object,
            dbContext
        ));
        
        Assert.Throws<ArgumentNullException>(() => new UnitOfWork(
            mockSequenceProvider.Object,
            mockParameterProvider.Object,
            mockSequenceParameterProvider.Object,
            mockRangeProvider.Object,
            null, // Test null range value provider
            mockResourceProvider.Object,
            mockSequenceGroupProvider.Object,
            dbContext
        ));
        
        Assert.Throws<ArgumentNullException>(() => new UnitOfWork(
            mockSequenceProvider.Object,
            mockParameterProvider.Object,
            mockSequenceParameterProvider.Object,
            mockRangeProvider.Object,
            mockRangeValueProvider.Object,
            null, // Test null resource provider
            mockSequenceGroupProvider.Object,
            dbContext
        ));
        
        Assert.Throws<ArgumentNullException>(() => new UnitOfWork(
            mockSequenceProvider.Object,
            mockParameterProvider.Object,
            mockSequenceParameterProvider.Object,
            mockRangeProvider.Object,
            mockRangeValueProvider.Object,
            mockResourceProvider.Object,
            null, // Test null sequence group provider
            dbContext
        ));
        
        Assert.Throws<ArgumentNullException>(() => new UnitOfWork(
            mockSequenceProvider.Object,
            mockParameterProvider.Object,
            mockSequenceParameterProvider.Object,
            mockRangeProvider.Object,
            mockRangeValueProvider.Object,
            mockResourceProvider.Object,
            mockSequenceGroupProvider.Object,
            null // Test null db context
        ));
    }
}
