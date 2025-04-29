using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Repository;
using Moq;

namespace Instrument.Scheduling.Data.UT;
public class RangeValueRepositoryTests
{
    private readonly Mock<IStorageProvider<RangeValue>> _mockProvider;
    private readonly RangeValueRepository _repository;
    
    public RangeValueRepositoryTests()
    {
        _mockProvider = new Mock<IStorageProvider<RangeValue>>();
        _repository = new RangeValueRepository(_mockProvider.Object);
    }
    
    [Fact]
    public async Task GetAllAsync_CallsProvider_AndReturnsResult()
    {
        // Arrange
        var rangeValues = new List<RangeValue>
        {
            new RangeValue { Id = "rv1", RangeId = "range1", Name = "Value 1", Value = "1" },
            new RangeValue { Id = "rv2", RangeId = "range1", Name = "Value 2", Value = "2" }
        };
        
        _mockProvider.Setup(p => p.GetAllAsync())
            .ReturnsAsync(rangeValues);
        
        // Act
        var result = await _repository.GetAllAsync();
        
        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, rv => rv.Id == "rv1");
        Assert.Contains(result, rv => rv.Id == "rv2");
        _mockProvider.Verify(p => p.GetAllAsync(), Times.Once);
    }
    
    [Fact]
    public async Task GetByIdAsync_CallsProvider_WithCorrectId()
    {
        // Arrange
        var rangeValue = new RangeValue { 
            Id = "test-id", 
            RangeId = "range1",
            Name = "Test Value",
            Value = "test"
        };
        
        _mockProvider.Setup(p => p.GetByIdAsync("test-id"))
            .ReturnsAsync(rangeValue);
        
        // Act
        var result = await _repository.GetByIdAsync("test-id");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-id", result.Id);
        Assert.Equal("range1", result.RangeId);
        Assert.Equal("Test Value", result.Name);
        Assert.Equal("test", result.Value);
        _mockProvider.Verify(p => p.GetByIdAsync("test-id"), Times.Once);
    }
    
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenRangeValueNotFound()
    {
        // Arrange
        _mockProvider.Setup(p => p.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((RangeValue)null);
        
        // Act
        var result = await _repository.GetByIdAsync("non-existent");
        
        // Assert
        Assert.Null(result);
        _mockProvider.Verify(p => p.GetByIdAsync("non-existent"), Times.Once);
    }
    
    [Fact]
    public async Task GetQueryableAsync_ReturnsQueryableData()
    {
        // Arrange
        var rangeValues = new List<RangeValue>
        {
            new RangeValue { Id = "rv1", RangeId = "range1", Name = "Alpha Value", Value = "a" },
            new RangeValue { Id = "rv2", RangeId = "range1", Name = "Beta Value", Value = "b" },
            new RangeValue { Id = "rv3", RangeId = "range2", Name = "Gamma Value", Value = "g" }
        };
        
        _mockProvider.Setup(p => p.GetAllAsync())
            .ReturnsAsync(rangeValues);
        
        // Act
        var result = await _repository.GetQueryableAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        
        // Test queryability
        var filtered = result.Where(rv => rv.Name.StartsWith("B")).ToList();
        Assert.Single(filtered);
        Assert.Equal("Beta Value", filtered[0].Name);
        
        _mockProvider.Verify(p => p.GetAllAsync(), Times.Once);
    }
    
    [Fact]
    public async Task GetValuesForRangeAsync_ReturnsCorrectValues()
    {
        // Arrange
        string rangeId = "range1";
        
        var rangeValues = new List<RangeValue>
        {
            new RangeValue { Id = "rv1", RangeId = rangeId, Name = "Value 1", Value = "1" },
            new RangeValue { Id = "rv2", RangeId = rangeId, Name = "Value 2", Value = "2" },
            new RangeValue { Id = "rv3", RangeId = "range2", Name = "Value 3", Value = "3" } // Different range
        };
        
        _mockProvider.Setup(p => p.GetAllAsync())
            .ReturnsAsync(rangeValues);
        
        // Act
        var result = await _repository.GetValuesForRangeAsync(rangeId);
        
        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, rv => rv.Id == "rv1");
        Assert.Contains(result, rv => rv.Id == "rv2");
        Assert.DoesNotContain(result, rv => rv.Id == "rv3");
        
        _mockProvider.Verify(p => p.GetAllAsync(), Times.Once);
    }
    
    [Fact]
    public async Task AddAsync_CallsProvider_WithCorrectEntity()
    {
        // Arrange
        var rangeValue = new RangeValue { 
            Id = "new-id", 
            RangeId = "range1",
            Name = "New Value",
            Value = "new"
        };
        
        // Act
        await _repository.AddAsync(rangeValue);
        
        // Assert
        _mockProvider.Verify(p => p.AddAsync(rangeValue), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAsync_CallsProvider_WithCorrectEntity()
    {
        // Arrange
        var rangeValue = new RangeValue { 
            Id = "update-id", 
            RangeId = "range1",
            Name = "Updated Value",
            Value = "updated"
        };
        
        // Act
        await _repository.UpdateAsync(rangeValue);
        
        // Assert
        _mockProvider.Verify(p => p.UpdateAsync(rangeValue), Times.Once);
    }
    
    [Fact]
    public async Task DeleteAsync_CallsProvider_WithCorrectId()
    {
        // Arrange
        string idToDelete = "delete-id";
        
        // Act
        await _repository.DeleteAsync(idToDelete);
        
        // Assert
        _mockProvider.Verify(p => p.DeleteAsync(idToDelete), Times.Once);
    }
    
    [Fact]
    public async Task SaveChangesAsync_CallsProvider_SaveChanges()
    {
        // Act
        await _repository.SaveChangesAsync();
        
        // Assert
        _mockProvider.Verify(p => p.SaveChangesAsync(), Times.Once);
    }
}