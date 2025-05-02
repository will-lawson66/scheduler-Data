using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Entities.Enums;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Repository;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Scheduling.Data.UT;

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
    public async Task GetRangeWithValuesAsync_ReturnsRangeWithValues()
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
            new RangeValue { Id = "val1", RangeId = "range1", Name = "Value 1", Value = "One" },
            new RangeValue { Id = "val2", RangeId = "range1", Name = "Value 2", Value = "Two" }
        };
        
        await _dbContext.RangeValues.AddRangeAsync(rangeValues);
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetRangeWithValuesAsync("range1");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("range1", result.Id);
        Assert.NotNull(result.Values);
        Assert.Equal(2, result.Values.Count);
        Assert.Contains(result.Values, v => v.Id == "val1");
        Assert.Contains(result.Values, v => v.Id == "val2");
    }
    
    [Fact]
    public async Task GetRangesByParameterAsync_ReturnsCorrectRanges()
    {
        // Arrange
        var parameterId = "param1";
        
        // Create ranges
        var ranges = new List<Entities.Range>
        {
            new Entities.Range { Id = "range1", Name = "Range 1", Description = "Range 1 description" },
            new Entities.Range { Id = "range2", Name = "Range 2", Description = "Range 2 description" }
        };
        
        await _dbContext.Ranges.AddRangeAsync(ranges);
        await _dbContext.SaveChangesAsync();
        
        // Create parameters
        var parameters = new List<Parameter>
        {
            new Parameter 
            { 
                Id = parameterId, 
                Name = "Parameter 1", 
                Type = ParameterType.StringType,
                RangeId = "range1"
            },
            new Parameter 
            { 
                Id = "param2", 
                Name = "Parameter 2", 
                Type = ParameterType.IntegerType,
                RangeId = "range2"
            }
        };
        
        await _dbContext.Parameters.AddRangeAsync(parameters);
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetRangesByParameterAsync(parameterId);
        
        // Assert
        Assert.Single(result);
        Assert.Equal("range1", result.First().Id);
    }
    
    [Fact]
    public async Task AddRangeValueAsync_AddsRangeValue()
    {
        // Arrange
        var rangeId = "range1";
        var name = "New Value";
        var value = "New";
        
        var range = new Entities.Range 
        { 
            Id = rangeId, 
            Name = "Range 1", 
            Description = "Range 1 description" 
        };
        
        await _dbContext.Ranges.AddAsync(range);
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.AddRangeValueAsync(rangeId, name, value);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(rangeId, result.RangeId);
        Assert.Equal(name, result.Name);
        Assert.Equal(value, result.Value);
        
        // Check in database
        var rangeValue = await _dbContext.RangeValues.FirstOrDefaultAsync(rv => 
            rv.RangeId == rangeId && rv.Name == name && rv.Value == value);
        Assert.NotNull(rangeValue);
    }
    
    [Fact]
    public async Task RemoveRangeValueAsync_RemovesRangeValue()
    {
        // Arrange
        var rangeId = "range1";
        var rangeValueId = "val1";
        
        var range = new Entities.Range 
        { 
            Id = rangeId, 
            Name = "Range 1", 
            Description = "Range 1 description" 
        };
        
        await _dbContext.Ranges.AddAsync(range);
        await _dbContext.SaveChangesAsync();
        
        var rangeValue = new RangeValue 
        { 
            Id = rangeValueId, 
            RangeId = rangeId, 
            Name = "Value 1", 
            Value = "One" 
        };
        
        await _dbContext.RangeValues.AddAsync(rangeValue);
        await _dbContext.SaveChangesAsync();
        
        // Act
        await _repository.RemoveRangeValueAsync(rangeValueId);
        
        // Assert
        var result = await _dbContext.RangeValues.FindAsync(rangeValueId);
        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetParametersForRangeAsync_ReturnsCorrectParameters()
    {
        // Arrange
        var rangeId = "range1";
        
        // Create range
        var range = new Entities.Range 
        { 
            Id = rangeId, 
            Name = "Range 1", 
            Description = "Range 1 description" 
        };
        
        await _dbContext.Ranges.AddAsync(range);
        await _dbContext.SaveChangesAsync();
        
        // Create parameters
        var parameters = new List<Parameter>
        {
            new Parameter { Id = "param1", Name = "Parameter 1", Type = ParameterType.StringType, RangeId = rangeId },
            new Parameter { Id = "param2", Name = "Parameter 2", Type = ParameterType.IntegerType, RangeId = rangeId },
            new Parameter { Id = "param3", Name = "Parameter 3", Type = ParameterType.BooleanType, RangeId = "range2" }
        };
        
        await _dbContext.Parameters.AddRangeAsync(parameters);
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetParametersForRangeAsync(rangeId);
        
        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, p => p.Id == "param1");
        Assert.Contains(result, p => p.Id == "param2");
        Assert.DoesNotContain(result, p => p.Id == "param3");
    }
}
