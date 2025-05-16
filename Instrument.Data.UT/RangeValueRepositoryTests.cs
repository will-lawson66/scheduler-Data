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
            new RangeValue { RangeId = 2, Name = "Value 1", Value = "1" },
            new RangeValue { RangeId = 3, Name = "Value 2", Value = "2" },
            new RangeValue { RangeId = 4, Name = "Value 3", Value = "3" }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        var rangeValues = result.ToList();
        Assert.Equal(3, rangeValues.Count);
        Assert.Contains(rangeValues, rv => rv.Value == "1");
        Assert.Contains(rangeValues, rv => rv.Value == "2");
        Assert.Contains(rangeValues, rv => rv.Value == "3");
    }
    
    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsRangeValue()
    {
        // Arrange
        var rangeValue = new RangeValue
        { 
            RangeId = 2,
            Name = "Test Value", 
            Value = "test"
        };
        
        await _dbContext.RangeValues.AddAsync(rangeValue);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(rangeValue.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.RangeId);
        Assert.Equal("Test Value", result.Name);
        Assert.Equal("test", result.Value);
    }
    
    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(-5);
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetQueryableAsync_ReturnsQueryable()
    {
        // Arrange
        await _dbContext.RangeValues.AddRangeAsync(
            new RangeValue { RangeId = 2, Name = "Value 1", Value = "1" },
            new RangeValue { RangeId = 2, Name = "Value 2", Value = "2" }
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
            RangeId = 2,
            Name = "New Value",
            Value = "new"
        };
        
        // Act
        await _repository.AddAsync(rangeValue);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _dbContext.RangeValues.FindAsync(rangeValue.Id);
        Assert.NotNull(result);
        Assert.Equal(2, result.RangeId);
        Assert.Equal("New Value", result.Name);
        Assert.Equal("new", result.Value);
    }
    
    [Fact]
    public async Task UpdateAsync_UpdatesRangeValue()
    {
        // Arrange
        var original = new RangeValue
        { 
            RangeId = 2,
            Name = "Original Value",
            Value = "original"
        };
        
        await _dbContext.RangeValues.AddAsync(original);
        await _dbContext.SaveChangesAsync();

        var update = original.Update(name: "Updated Value", value: "Updated");

        // Act
        await _repository.UpdateAsync(update);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _dbContext.RangeValues.FindAsync(original.Id);
        Assert.NotNull(result);
        Assert.Equal("Updated Value", result.Name);
        Assert.Equal("Updated", result.Value);
    }
    
    [Fact]
    public async Task DeleteAsync_DeletesRangeValue()
    {
        // Arrange
        var rangeValue = new RangeValue
        { 
            RangeId = 2,
            Name = "Delete Value",
            Value = "delete"
        };
        
        await _dbContext.RangeValues.AddAsync(rangeValue);
        await _dbContext.SaveChangesAsync();
        
        // Act
        await _repository.DeleteAsync(rangeValue.Id);
        await _repository.SaveChangesAsync();
        
        // Assert
        var result = await _dbContext.RangeValues.FindAsync(rangeValue.Id);
        Assert.Null(result);
    }
    
    [Fact]
    public async Task DeleteAsync_WithInvalidId_ThrowsEntityNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _repository.DeleteAsync(-5));
    }
    
    [Fact]
    public async Task GetValuesByRangeIdAsync_ReturnsCorrectValues()
    {
        // Arrange
        var rangeId = 2;
        
        await _dbContext.RangeValues.AddRangeAsync(
            new RangeValue { RangeId = rangeId, Name = "Value 1", Value = "1" },
            new RangeValue { RangeId = rangeId, Name = "Value 2", Value = "2" },
            new RangeValue { RangeId = 3, Name = "Value 3", Value = "3" }
        );
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetRangeValuesByRangeIdAsync(rangeId);
        
        // Assert
        var enumerable = result.ToList();
        Assert.Equal(2, enumerable.Count);
        Assert.Contains(enumerable, rv => rv.Value == "1");
        Assert.Contains(enumerable, rv => rv.Value == "2");
        Assert.DoesNotContain(enumerable, rv => rv.Value == "3");
    }
}
