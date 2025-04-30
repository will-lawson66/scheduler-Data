using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Repository;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Instrument.Scheduling.Data.UT;
public class RangeValueRepositoryTests
{
    private readonly Mock<SchedulerDbContext> _mockDbContext;
    private readonly Mock<DbSet<RangeValue>> _mockRangeValueDbSet;
    private readonly RangeValueRepository _repository;
    
    public RangeValueRepositoryTests()
    {
        // Create mock DbSet
        _mockRangeValueDbSet = MockDbSetSetup();
        
        // Create mock DbContext
        _mockDbContext = new Mock<SchedulerDbContext>(new DbContextOptionsBuilder<SchedulerDbContext>().Options);
        _mockDbContext.Setup(db => db.RangeValues).Returns(_mockRangeValueDbSet.Object);
        
        // Create repository
        _repository = new RangeValueRepository(_mockDbContext.Object);
    }
    
    private Mock<DbSet<RangeValue>> MockDbSetSetup()
    {
        var rangeValues = new List<RangeValue>
        {
            new RangeValue { Id = "rv1", RangeId = "range1", Name = "Value 1", Value = "1" },
            new RangeValue { Id = "rv2", RangeId = "range1", Name = "Value 2", Value = "2" },
            new RangeValue { Id = "rv3", RangeId = "range2", Name = "Value 3", Value = "3" }
        }.AsQueryable();
        
        var mockSet = new Mock<DbSet<RangeValue>>();
        mockSet.As<IQueryable<RangeValue>>().Setup(m => m.Provider).Returns(rangeValues.Provider);
        mockSet.As<IQueryable<RangeValue>>().Setup(m => m.Expression).Returns(rangeValues.Expression);
        mockSet.As<IQueryable<RangeValue>>().Setup(m => m.ElementType).Returns(rangeValues.ElementType);
        mockSet.As<IQueryable<RangeValue>>().Setup(m => m.GetEnumerator()).Returns(rangeValues.GetEnumerator());
        
        mockSet.Setup(m => m.Find(It.IsAny<object[]>())).Returns<object[]>(ids => 
        {
            var id = ids[0].ToString();
            return rangeValues.FirstOrDefault(rv => rv.Id == id);
        });
        
        return mockSet;
    }
    
    [Fact]
    public async Task GetAllAsync_CallsDbContext_AndReturnsResult()
    {
        // Arrange
        _mockRangeValueDbSet.Setup(m => m.ToListAsync(default))
            .ReturnsAsync(new List<RangeValue>
            {
                new RangeValue { Id = "rv1", RangeId = "range1", Name = "Value 1", Value = "1" },
                new RangeValue { Id = "rv2", RangeId = "range1", Name = "Value 2", Value = "2" },
                new RangeValue { Id = "rv3", RangeId = "range2", Name = "Value 3", Value = "3" }
            });
        
        // Act
        var result = await _repository.GetAllAsync();
        
        // Assert
        Assert.Equal(3, result.Count());
        Assert.Contains(result, rv => rv.Id == "rv1");
        Assert.Contains(result, rv => rv.Id == "rv2");
        Assert.Contains(result, rv => rv.Id == "rv3");
        _mockRangeValueDbSet.Verify(m => m.ToListAsync(default), Times.Once);
    }
    
    [Fact]
    public async Task GetByIdAsync_CallsDbContext_WithCorrectId()
    {
        // Arrange
        var rangeValue = new RangeValue { 
            Id = "test-id", 
            RangeId = "range1",
            Name = "Test Value",
            Value = "test"
        };
        
        _mockRangeValueDbSet.Setup(m => m.FindAsync(new object[] { "test-id" }))
            .ReturnsAsync(rangeValue);
        
        // Act
        var result = await _repository.GetByIdAsync("test-id");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-id", result.Id);
        Assert.Equal("range1", result.RangeId);
        Assert.Equal("Test Value", result.Name);
        Assert.Equal("test", result.Value);
        _mockRangeValueDbSet.Verify(m => m.FindAsync(new object[] { "test-id" }), Times.Once);
    }
    
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenRangeValueNotFound()
    {
        // Arrange
        _mockRangeValueDbSet.Setup(m => m.FindAsync(new object[] { "non-existent" }))
            .ReturnsAsync((RangeValue)null);
        
        // Act
        var result = await _repository.GetByIdAsync("non-existent");
        
        // Assert
        Assert.Null(result);
        _mockRangeValueDbSet.Verify(m => m.FindAsync(new object[] { "non-existent" }), Times.Once);
    }
    
    [Fact]
    public async Task GetQueryableAsync_ReturnsQueryable()
    {
        // Act
        var result = await _repository.GetQueryableAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IQueryable<RangeValue>>(result);
    }
    
    [Fact]
    public async Task AddAsync_CallsDbContext_WithCorrectEntity()
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
        _mockRangeValueDbSet.Verify(m => m.AddAsync(rangeValue, default), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAsync_CallsDbContext_WithCorrectEntity()
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
        _mockRangeValueDbSet.Verify(m => m.Update(rangeValue), Times.Once);
    }
    
    [Fact]
    public async Task DeleteAsync_CallsDbContext_WithCorrectId()
    {
        // Arrange
        var rangeValue = new RangeValue { 
            Id = "delete-id", 
            RangeId = "range1",
            Name = "Delete Value",
            Value = "delete"
        };
        
        _mockRangeValueDbSet.Setup(m => m.FindAsync(new object[] { "delete-id" }))
            .ReturnsAsync(rangeValue);
        
        // Act
        await _repository.DeleteAsync("delete-id");
        
        // Assert
        _mockRangeValueDbSet.Verify(m => m.Remove(rangeValue), Times.Once);
    }
    
    [Fact]
    public async Task DeleteAsync_ThrowsException_WhenEntityNotFound()
    {
        // Arrange
        _mockRangeValueDbSet.Setup(m => m.FindAsync(new object[] { "non-existent" }))
            .ReturnsAsync((RangeValue)null);
        
        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _repository.DeleteAsync("non-existent"));
    }
    
    [Fact]
    public async Task SaveChangesAsync_CallsDbContext_SaveChanges()
    {
        // Act
        await _repository.SaveChangesAsync();
        
        // Assert
        _mockDbContext.Verify(m => m.SaveChangesAsync(default), Times.Once);
    }
    
    [Fact]
    public async Task GetValuesByRangeIdAsync_ReturnsCorrectValues()
    {
        // Arrange
        string rangeId = "range1";
        
        var rangeValues = new List<RangeValue>
        {
            new RangeValue { Id = "rv1", RangeId = rangeId, Name = "Value 1", Value = "1" },
            new RangeValue { Id = "rv2", RangeId = rangeId, Name = "Value 2", Value = "2" },
            new RangeValue { Id = "rv3", RangeId = "range2", Name = "Value 3", Value = "3" }
        }.AsQueryable();
        
        var mockDbSet = new Mock<DbSet<RangeValue>>();
        mockDbSet.As<IQueryable<RangeValue>>().Setup(m => m.Provider).Returns(rangeValues.Provider);
        mockDbSet.As<IQueryable<RangeValue>>().Setup(m => m.Expression).Returns(rangeValues.Expression);
        mockDbSet.As<IQueryable<RangeValue>>().Setup(m => m.ElementType).Returns(rangeValues.ElementType);
        mockDbSet.As<IQueryable<RangeValue>>().Setup(m => m.GetEnumerator()).Returns(rangeValues.GetEnumerator());
        
        _mockDbContext.Setup(db => db.RangeValues).Returns(mockDbSet.Object);
        
        // Act
        var result = await _repository.GetValuesByRangeIdAsync(rangeId);
        
        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, rv => rv.Id == "rv1");
        Assert.Contains(result, rv => rv.Id == "rv2");
        Assert.DoesNotContain(result, rv => rv.Id == "rv3");
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
            new RangeValue { Id = "rv3", RangeId = "range2", Name = "Value 3", Value = "3" }
        }.AsQueryable();
        
        var mockDbSet = new Mock<DbSet<RangeValue>>();
        mockDbSet.As<IQueryable<RangeValue>>().Setup(m => m.Provider).Returns(rangeValues.Provider);
        mockDbSet.As<IQueryable<RangeValue>>().Setup(m => m.Expression).Returns(rangeValues.Expression);
        mockDbSet.As<IQueryable<RangeValue>>().Setup(m => m.ElementType).Returns(rangeValues.ElementType);
        mockDbSet.As<IQueryable<RangeValue>>().Setup(m => m.GetEnumerator()).Returns(rangeValues.GetEnumerator());
        
        _mockDbContext.Setup(db => db.RangeValues).Returns(mockDbSet.Object);
        
        // Act
        var result = await _repository.GetValuesForRangeAsync(rangeId);
        
        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, rv => rv.Id == "rv1");
        Assert.Contains(result, rv => rv.Id == "rv2");
        Assert.DoesNotContain(result, rv => rv.Id == "rv3");
    }
    
    [Fact]
    public async Task GetByNameAndRangeIdAsync_ReturnsCorrectValue()
    {
        // Arrange
        string rangeId = "range1";
        string name = "Value 1";
        
        var rangeValues = new List<RangeValue>
        {
            new RangeValue { Id = "rv1", RangeId = rangeId, Name = name, Value = "1" },
            new RangeValue { Id = "rv2", RangeId = rangeId, Name = "Value 2", Value = "2" }
        }.AsQueryable();
        
        var mockDbSet = new Mock<DbSet<RangeValue>>();
        mockDbSet.As<IQueryable<RangeValue>>().Setup(m => m.Provider).Returns(rangeValues.Provider);
        mockDbSet.As<IQueryable<RangeValue>>().Setup(m => m.Expression).Returns(rangeValues.Expression);
        mockDbSet.As<IQueryable<RangeValue>>().Setup(m => m.ElementType).Returns(rangeValues.ElementType);
        mockDbSet.As<IQueryable<RangeValue>>().Setup(m => m.GetEnumerator()).Returns(rangeValues.GetEnumerator());
        
        _mockDbContext.Setup(db => db.RangeValues).Returns(mockDbSet.Object);
        
        // Act
        var result = await _repository.GetByNameAndRangeIdAsync(name, rangeId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("rv1", result.Id);
        Assert.Equal(name, result.Name);
        Assert.Equal(rangeId, result.RangeId);
    }
    
    [Fact]
    public async Task UpdateRangeValueAsync_UpdatesExistingValue()
    {
        // Arrange
        string id = "rv1";
        string name = "Updated Name";
        string value = "updated";
        
        var rangeValue = new RangeValue { Id = id, RangeId = "range1", Name = "Original Name", Value = "original" };
        
        _mockRangeValueDbSet.Setup(m => m.FindAsync(new object[] { id }))
            .ReturnsAsync(rangeValue);
        
        // Act
        await _repository.UpdateRangeValueAsync(id, name, value);
        
        // Assert
        Assert.Equal(name, rangeValue.Name);
        Assert.Equal(value, rangeValue.Value);
        _mockRangeValueDbSet.Verify(m => m.Update(rangeValue), Times.Once);
        _mockDbContext.Verify(m => m.SaveChangesAsync(default), Times.Once);
    }
    
    [Fact]
    public async Task UpdateRangeValueAsync_ThrowsException_WhenValueNotFound()
    {
        // Arrange
        string id = "non-existent";
        
        _mockRangeValueDbSet.Setup(m => m.FindAsync(new object[] { id }))
            .ReturnsAsync((RangeValue)null);
        
        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _repository.UpdateRangeValueAsync(id, "new name", "new value"));
    }
}