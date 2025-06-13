namespace Instrument.Scheduling.Data.UT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Repository;
using Instrument.Scheduling.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

public class RangeServiceTests : IDisposable
{
    private readonly SchedulerDbContext _dbContext;
    private readonly IRangeRepository _rangeRepository;
    private readonly Mock<ILogger<RangeService>> _mockLogger;
    private readonly RangeService _service;
    private readonly string _dbName;

    public RangeServiceTests()
    {
        _dbName = $"TestDb_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: _dbName)
            .Options;

        _dbContext = new SchedulerDbContext(options);
        _rangeRepository = new RangeRepository(_dbContext);
        _mockLogger = new Mock<ILogger<RangeService>>();
        _service = new RangeService(_rangeRepository, _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetRangeByIdAsync_WithValidId_ReturnsRange()
    {
        // Arrange
        var range = new Entities.Range
        {
            Name = "Test Range",
            Description = "Test Description"
        };

        await _dbContext.Ranges.AddAsync(range);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetRangeByIdAsync(range.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(range.Id, result.Id);
        Assert.Equal("Test Range", result.Name);
        Assert.Equal("Test Description", result.Description);
    }

    [Fact]
    public async Task GetRangeByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _service.GetRangeByIdAsync(-1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRangeWithRangeValuesAsync_WithValidId_ReturnsRangeWithValues()
    {
        // Arrange
        var range = new Entities.Range
        {
            Name = "Test Range",
            Description = "Test Description"
        };

        await _dbContext.Ranges.AddAsync(range);
        await _dbContext.SaveChangesAsync();

        var rangeValues = new List<RangeValue>
            {
                new() { RangeId = range.Id, Name = "Value 1", Value = "1" },
                new() { RangeId = range.Id, Name = "Value 2", Value = "2" }
            };

        await _dbContext.RangeValues.AddRangeAsync(rangeValues);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetRangeWithRangeValuesAsync(range.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(range.Id, result.Id);
        Assert.NotNull(result.RangeValues);
        Assert.Equal(2, result.RangeValues.Count);
    }

    [Fact]
    public async Task CreateRangeAsync_WithValidRange_CreatesRange()
    {
        // Arrange
        var range = new Entities.Range
        {
            Name = "New Range",
            Description = "New Description"
        };

        // Act
        var result = await _service.CreateRangeAsync(range);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Range", result.Name);
        Assert.Equal("New Description", result.Description);
        Assert.True(result.Id > 0);

        var createdRange = await _dbContext.Ranges.FindAsync(result.Id);
        Assert.NotNull(createdRange);
        Assert.Equal("New Range", createdRange.Name);
    }

    [Fact]
    public async Task CreateRangeAsync_WithNullRange_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.CreateRangeAsync(null!));
    }

    [Fact]
    public async Task UpdateRangeAsync_WithValidRange_UpdatesRange()
    {
        // Arrange
        var existingRange = new Entities.Range
        {
            Name = "Original Range",
            Description = "Original Description"
        };

        await _dbContext.Ranges.AddAsync(existingRange);
        await _dbContext.SaveChangesAsync();

        var updatedRange = existingRange.Update("Updated Range", "Updated Description");

        // Act
        await _service.UpdateRangeAsync(updatedRange);

        // Assert
        var resultRange = await _dbContext.Ranges.FindAsync(existingRange.Id);
        Assert.NotNull(resultRange);
        Assert.Equal("Updated Range", resultRange.Name);
        Assert.Equal("Updated Description", resultRange.Description);
    }

    [Fact]
    public async Task UpdateRangeAsync_WithNullRange_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.UpdateRangeAsync(null!));
    }

    [Fact]
    public async Task DeleteRangeAsync_WithValidId_DeletesRange()
    {
        // Arrange
        var range = new Entities.Range
        {
            Name = "Range to Delete",
            Description = "Delete Description"
        };

        await _dbContext.Ranges.AddAsync(range);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.DeleteRangeAsync(range.Id);

        // Assert
        var deletedRange = await _dbContext.Ranges.FindAsync(range.Id);
        Assert.Null(deletedRange);
    }

    [Fact]
    public async Task DeleteRangeAsync_WithInvalidId_ThrowsEntityNotFoundException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _service.DeleteRangeAsync(-1));

        Assert.Equal(-1, exception.EntityId);
        Assert.Equal("Range", exception.EntityType);
    }

    [Fact]
    public async Task GetAllRangesAsync_ReturnsAllRanges()
    {
        // Arrange
        var ranges = new List<Entities.Range>
            {
                new() { Name = "Range 1", Description = "Description 1" },
                new() { Name = "Range 2", Description = "Description 2" }
            };

        await _dbContext.Ranges.AddRangeAsync(ranges);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetAllRangesAsync();

        // Assert
        var rangeList = result.ToList();
        Assert.Equal(2, rangeList.Count);
        Assert.Contains(rangeList, r => r.Name == "Range 1");
        Assert.Contains(rangeList, r => r.Name == "Range 2");
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RangeService(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RangeService(_rangeRepository, null!));
    }
}
