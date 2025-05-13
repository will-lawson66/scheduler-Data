using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Instrument.Data.Exceptions;
using Instrument.Data.Repository;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Data.UT;

public class RangeValueRepositoryTests : IDisposable
{
    private readonly SchedulerDbContext _dbContext;
    private readonly RangeValueRepository _repository;
    private readonly string _dbName;

    public RangeValueRepositoryTests()
    {
        // Create a unique database name for each test run to ensure isolation
        _dbName = $"TestDB_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: _dbName)
            .Options;
        
        _dbContext = new SchedulerDbContext(options);
        _repository = new RangeValueRepository(_dbContext);
    }

    public void Dispose()
    {
        // Clean up database after test
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
    
    [Fact]
    public async Task GetAllAsync_ReturnsAllRangeValues()
    {
        // Arrange
        await _dbContext.RangeValues.AddRangeAsync(
            new RangeValue { Id = "rv1", RangeId = "range1", Name = "Value 1", Value = "1" },
            new RangeValue { Id = "rv2", RangeId = "range1", Name = "Value 2", Value = "2" },
            new RangeValue { Id = "rv3", RangeId = "range2", Name = "Value 3", Value = "3" }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        var rangeValues = result.ToList();
        Assert.Equal(3, rangeValues.Count);
        Assert.Contains(rangeValues, rv => rv.Id == "rv1");
        Assert.Contains(rangeValues, rv => rv.Id == "rv2");
        Assert.Contains(rangeValues, rv => rv.Id == "rv3");
    }
    
    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsRangeValue()
    {
        // Arrange
        var rangeValue = new RangeValue
        { 
            Id = "rv-id",
            RangeId = "range1",
            Name = "Test Value", 
            Value = "test"
        };
        
        await _dbContext.RangeValues.AddAsync(rangeValue);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync("rv-id");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("rv-id", result.Id);
        Assert.Equal("range1", result.RangeId);
        Assert.Equal("Test Value", result.Name);
        Assert.Equal("test", result.Value);
    }
    
    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync("non-existent");
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetQueryableAsync_ReturnsQueryable()
    {
        // Arrange
        await _dbContext.RangeValues.AddRangeAsync(
            new RangeValue { Id = "rv1", RangeId = "range1", Name = "Value 1", Value = "1" },
            new RangeValue { Id = "rv2", RangeId = "range1", Name = "Value 2", Value = "2" }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetQueryableAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IQueryable<RangeValue>>(result);
        Assert.Equal(2, result.Count());
    }
    
    [Fact]
    public async Task AddAsync_AddsRangeValue()
    {
        // Arrange
        var rangeValue = new RangeValue
        { 
            Id = "new-id", 
            RangeId = "range1",
            Name = "New Value",
            Value = "new"
        };
        
        // Act
        await _repository.AddAsync(rangeValue);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _dbContext.RangeValues.FindAsync("new-id");
        Assert.NotNull(result);
        Assert.Equal("range1", result.RangeId);
        Assert.Equal("New Value", result.Name);
        Assert.Equal("new", result.Value);
    }
    
    [Fact]
    public async Task UpdateAsync_UpdatesRangeValue()
    {
        // Arrange
        var original = new RangeValue
        { 
            Id = "rv-id", 
            RangeId = "range1",
            Name = "Original Value",
            Value = "original"
        };
        
        await _dbContext.RangeValues.AddAsync(original);
        await _dbContext.SaveChangesAsync();
        
        var updated = new RangeValue
        { 
            Id = "rv-id", 
            RangeId = "range1",
            Name = "Updated Value",
            Value = "updated"
        };

        // Act
        await _repository.UpdateAsync(updated);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _dbContext.RangeValues.FindAsync("rv-id");
        Assert.NotNull(result);
        Assert.Equal("Updated Value", result.Name);
        Assert.Equal("updated", result.Value);
    }
    
    [Fact]
    public async Task DeleteAsync_DeletesRangeValue()
    {
        // Arrange
        var rangeValue = new RangeValue
        { 
            Id = "delete-id", 
            RangeId = "range1",
            Name = "Delete Value",
            Value = "delete"
        };
        
        await _dbContext.RangeValues.AddAsync(rangeValue);
        await _dbContext.SaveChangesAsync();
        
        // Act
        await _repository.DeleteAsync("delete-id");
        await _repository.SaveChangesAsync();
        
        // Assert
        var result = await _dbContext.RangeValues.FindAsync("delete-id");
        Assert.Null(result);
    }
    
    [Fact]
    public async Task DeleteAsync_WithInvalidId_ThrowsEntityNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _repository.DeleteAsync("non-existent"));
    }
    
    [Fact]
    public async Task GetValuesByRangeIdAsync_ReturnsCorrectValues()
    {
        // Arrange
        var rangeId = "range1";
        
        await _dbContext.RangeValues.AddRangeAsync(
            new RangeValue { Id = "rv1", RangeId = rangeId, Name = "Value 1", Value = "1" },
            new RangeValue { Id = "rv2", RangeId = rangeId, Name = "Value 2", Value = "2" },
            new RangeValue { Id = "rv3", RangeId = "range2", Name = "Value 3", Value = "3" }
        );
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetRangeValuesByRangeIdAsync(rangeId);
        
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
        var rangeId = "range1";
        
        await _dbContext.RangeValues.AddRangeAsync(
            new RangeValue { Id = "rv1", RangeId = rangeId, Name = "Value 1", Value = "1" },
            new RangeValue { Id = "rv2", RangeId = rangeId, Name = "Value 2", Value = "2" },
            new RangeValue { Id = "rv3", RangeId = "range2", Name = "Value 3", Value = "3" }
        );
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetRangeValuesByRangeIdAsync(rangeId);
        
        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, rv => rv.Id == "rv1");
        Assert.Contains(result, rv => rv.Id == "rv2");
        Assert.DoesNotContain(result, rv => rv.Id == "rv3");
    }
    
    
    
}
