using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Repository;
using Moq;

namespace Instrument.Scheduling.Data.UT;
public class RangeRepositoryTests
{
    private readonly Mock<IStorageProvider<Entities.Range>> _mockRangeProvider;
    private readonly Mock<IStorageProvider<Parameter>> _mockParameterProvider;
    private readonly RangeRepository _repository;
    
    public RangeRepositoryTests()
    {
        _mockRangeProvider = new Mock<IStorageProvider<Entities.Range>>();
        _mockParameterProvider = new Mock<IStorageProvider<Parameter>>();
        _repository = new RangeRepository(
            _mockRangeProvider.Object,
            _mockParameterProvider.Object);
    }
    
    [Fact]
    public async Task GetAllAsync_CallsProvider_AndReturnsResult()
    {
        // Arrange
        var ranges = new List<Entities.Range>
        {
            new Entities.Range { Id = "range1", Name = "Range 1" },
            new Entities.Range { Id = "range2", Name = "Range 2" }
        };
        
        _mockRangeProvider.Setup(p => p.GetAllAsync())
            .ReturnsAsync(ranges);
        
        // Act
        var result = await _repository.GetAllAsync();
        
        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, r => r.Id == "range1");
        Assert.Contains(result, r => r.Id == "range2");
        _mockRangeProvider.Verify(p => p.GetAllAsync(), Times.Once);
    }
    
    [Fact]
    public async Task GetByIdAsync_CallsProvider_WithCorrectId()
    {
        // Arrange
        var range = new Entities.Range { 
            Id = "test-id", 
            Name = "Test Range",
            Description = "Test Description"
        };
        
        _mockRangeProvider.Setup(p => p.GetByIdAsync("test-id"))
            .ReturnsAsync(range);
        
        // Act
        var result = await _repository.GetByIdAsync("test-id");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-id", result.Id);
        Assert.Equal("Test Range", result.Name);
        Assert.Equal("Test Description", result.Description);
        _mockRangeProvider.Verify(p => p.GetByIdAsync("test-id"), Times.Once);
    }
    
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenRangeNotFound()
    {
        // Arrange
        _mockRangeProvider.Setup(p => p.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Entities.Range)null);
        
        // Act
        var result = await _repository.GetByIdAsync("non-existent");
        
        // Assert
        Assert.Null(result);
        _mockRangeProvider.Verify(p => p.GetByIdAsync("non-existent"), Times.Once);
    }
    
    [Fact]
    public async Task GetQueryableAsync_ReturnsQueryableData()
    {
        // Arrange
        var ranges = new List<Entities.Range>
        {
            new Entities.Range { Id = "range1", Name = "Alpha Range" },
            new Entities.Range { Id = "range2", Name = "Beta Range" },
            new Entities.Range { Id = "range3", Name = "Gamma Range" }
        };
        
        _mockRangeProvider.Setup(p => p.GetAllAsync())
            .ReturnsAsync(ranges);
        
        // Act
        var result = await _repository.GetQueryableAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        
        // Test queryability
        var filtered = result.Where(r => r.Name.StartsWith("B")).ToList();
        Assert.Single(filtered);
        Assert.Equal("Beta Range", filtered[0].Name);
        
        _mockRangeProvider.Verify(p => p.GetAllAsync(), Times.Once);
    }
    
    [Fact]
    public async Task AddAsync_CallsProvider_WithCorrectEntity()
    {
        // Arrange
        var range = new Entities.Range { 
            Id = "new-id", 
            Name = "New Range",
            Description = "New Description"
        };
        
        // Act
        await _repository.AddAsync(range);
        
        // Assert
        _mockRangeProvider.Verify(p => p.AddAsync(range), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAsync_CallsProvider_WithCorrectEntity()
    {
        // Arrange
        var range = new Entities.Range { 
            Id = "update-id", 
            Name = "Updated Range",
            Description = "Updated Description"
        };
        
        // Act
        await _repository.UpdateAsync(range);
        
        // Assert
        _mockRangeProvider.Verify(p => p.UpdateAsync(range), Times.Once);
    }
    
    [Fact]
    public async Task DeleteAsync_CallsProvider_WithCorrectId()
    {
        // Arrange
        string idToDelete = "delete-id";
        
        // Act
        await _repository.DeleteAsync(idToDelete);
        
        // Assert
        _mockRangeProvider.Verify(p => p.DeleteAsync(idToDelete), Times.Once);
    }
    
    [Fact]
    public async Task SaveChangesAsync_CallsProvider_SaveChanges()
    {
        // Act
        await _repository.SaveChangesAsync();
        
        // Assert
        _mockRangeProvider.Verify(p => p.SaveChangesAsync(), Times.Once);
    }
    
    [Fact]
    public async Task GetParametersForRangeAsync_ReturnsCorrectParameters()
    {
        // Arrange
        string rangeId = "range1";
        
        var parameters = new List<Parameter>
        {
            new Parameter { 
                Id = "param1", 
                Name = "Parameter 1", 
                Type = "String",
                RangeId = rangeId
            },
            new Parameter { 
                Id = "param2", 
                Name = "Parameter 2", 
                Type = "Integer",
                RangeId = rangeId
            },
            new Parameter { 
                Id = "param3", 
                Name = "Parameter 3", 
                Type = "Boolean",
                RangeId = "different-range" // Not associated with the target range
            }
        };
        
        _mockParameterProvider.Setup(p => p.GetAllAsync())
            .ReturnsAsync(parameters);
        
        // Act
        var result = await _repository.GetParametersForRangeAsync(rangeId);
        
        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, p => p.Id == "param1");
        Assert.Contains(result, p => p.Id == "param2");
        Assert.DoesNotContain(result, p => p.Id == "param3");
        
        _mockParameterProvider.Verify(p => p.GetAllAsync(), Times.Once);
    }
}