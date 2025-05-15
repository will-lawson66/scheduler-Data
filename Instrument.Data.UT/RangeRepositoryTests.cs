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
    public async Task GetRangeWithRangeValuesAsync_ReturnsRangeWithValues()
    {
        // Arrange
        var range = new Entities.Range
        { 
            Name = "Range 1", 
            Description = "Range 1 description"
        };
        
        await _dbContext.Ranges.AddAsync(range);
        await _dbContext.SaveChangesAsync();
        
        var rangeValues = new List<RangeValue>
        {
            new()
                { RangeId = range.Id, Name = "Value 1", Value = "One" },
            new()
                { RangeId = range.Id, Name = "Value 2", Value = "Two" },
        };
        
        await _dbContext.RangeValues.AddRangeAsync(rangeValues);
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetRangeWithRangeValuesByIdAsync(range.Id);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(range.Id, result.Id);
        Assert.NotNull(result.RangeValues);
        Assert.Equal(2, result.RangeValues.Count);
        Assert.Contains(result.RangeValues, v => v.Name == "Value 1");
        Assert.Contains(result.RangeValues, v => v.Name == "Value 2");
    }
}
