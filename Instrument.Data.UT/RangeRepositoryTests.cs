using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Instrument.Data.Exceptions;
using Instrument.Data.Repository;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Data.UT;

public class RangeRepositoryTests : IDisposable
{
    private readonly SchedulerDbContext _dbContext;
    private readonly RangeRepository _repository;
    private readonly string _dbName;

    public RangeRepositoryTests()
    {
        // Create a unique database name for each test run to ensure isolation
        _dbName = $"TestDB_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: _dbName)
            .Options;
        
        _dbContext = new SchedulerDbContext(options);
        _repository = new RangeRepository(_dbContext);
    }

    public void Dispose()
    {
        // Clean up database after test
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
    
    [Fact]
    public async Task GetAllAsync_ReturnsAllRanges()
    {
        // Arrange
        await _dbContext.Ranges.AddRangeAsync(
            new Entities.Range { Id = "range1", Name = "Range 1", Description = "Range 1 description" },
            new Entities.Range { Id = "range2", Name = "Range 2", Description = "Range 2 description" }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        var ranges = result.ToList();
        Assert.Equal(2, ranges.Count);
        Assert.Contains(ranges, r => r.Id == "range1");
        Assert.Contains(ranges, r => r.Id == "range2");
    }
    
    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsRange()
    {
        // Arrange
        var range = new Entities.Range
        { 
            Id = "range-id",
            Name = "Test Range", 
            Description = "Test range description"
        };
        
        await _dbContext.Ranges.AddAsync(range);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync("range-id");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("range-id", result.Id);
        Assert.Equal("Test Range", result.Name);
        Assert.Equal("Test range description", result.Description);
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
        await _dbContext.Ranges.AddRangeAsync(
            new Entities.Range { Id = "range1", Name = "Range 1", Description = "Range 1 description" },
            new Entities.Range { Id = "range2", Name = "Range 2", Description = "Range 2 description" }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetQueryableAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IQueryable<Entities.Range>>(result);
        Assert.Equal(2, result.Count());
    }
    
    [Fact]
    public async Task AddAsync_AddsRange()
    {
        // Arrange
        var range = new Entities.Range
        { 
            Id = "new-id", 
            Name = "New Range",
            Description = "New range description"
        };
        
        // Act
        await _repository.AddAsync(range);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _dbContext.Ranges.FindAsync("new-id");
        Assert.NotNull(result);
        Assert.Equal("New Range", result.Name);
        Assert.Equal("New range description", result.Description);
    }
    
    [Fact]
    public async Task UpdateAsync_UpdatesRange()
    {
        // Arrange
        var original = new Entities.Range
        { 
            Id = "range-id", 
            Name = "Original Range",
            Description = "Original range description"
        };
        
        await _dbContext.Ranges.AddAsync(original);
        await _dbContext.SaveChangesAsync();
        
        var updated = new Entities.Range
        { 
            Id = "range-id", 
            Name = "Updated Range",
            Description = "Updated range description"
        };

        // Act
        await _repository.UpdateAsync(updated);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _dbContext.Ranges.FindAsync("range-id");
        Assert.NotNull(result);
        Assert.Equal("Updated Range", result.Name);
        Assert.Equal("Updated range description", result.Description);
    }
    
    [Fact]
    public async Task DeleteAsync_DeletesRange()
    {
        // Arrange
        var range = new Entities.Range
        { 
            Id = "delete-id", 
            Name = "Delete Range",
            Description = "Delete range description"
        };
        
        await _dbContext.Ranges.AddAsync(range);
        await _dbContext.SaveChangesAsync();
        
        // Act
        await _repository.DeleteAsync("delete-id");
        await _repository.SaveChangesAsync();
        
        // Assert
        var result = await _dbContext.Ranges.FindAsync("delete-id");
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
    public async Task GetRangeWithRangeValuesAsync_ReturnsRangeWithValues()
    {
        // Arrange
        var range = new Entities.Range
        { 
            Id = "range1", 
            Name = "Range 1", 
            Description = "Range 1 description"
        };
        
        await _dbContext.Ranges.AddAsync(range);
        await _dbContext.SaveChangesAsync();
        
        var rangeValues = new List<RangeValue>
        {
            new()
                { Id = "val1", RangeId = "range1", Name = "Value 1", Value = "One" },
            new()
                { Id = "val2", RangeId = "range1", Name = "Value 2", Value = "Two" },
        };
        
        await _dbContext.RangeValues.AddRangeAsync(rangeValues);
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetRangeWithRangeValuesByIdAsync("range1");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("range1", result.Id);
        Assert.NotNull(result.RangeValues);
        Assert.Equal(2, result.RangeValues.Count);
        Assert.Contains(result.RangeValues, v => v.Id == "val1");
        Assert.Contains(result.RangeValues, v => v.Id == "val2");
    }
}
