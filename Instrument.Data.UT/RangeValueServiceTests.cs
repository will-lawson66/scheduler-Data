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

public class RangeValueServiceTests : IDisposable
{
    private readonly SchedulerDbContext _dbContext;
    private readonly IRangeValueRepository _rangeValueRepository;
    private readonly Mock<ILogger<RangeValueService>> _mockLogger;
    private readonly RangeValueService _service;
    private readonly string _dbName;

    public RangeValueServiceTests()
    {
        _dbName = $"TestDb_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: _dbName)
            .Options;

        _dbContext = new SchedulerDbContext(options);
        _rangeValueRepository = new RangeValueRepository(_dbContext);
        _mockLogger = new Mock<ILogger<RangeValueService>>();
        _service = new RangeValueService(_rangeValueRepository, _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetRangeValueByIdAsync_WithValidId_ReturnsRangeValue()
    {
        // Arrange
        var rangeValue = new RangeValue
        {
            RangeId = 1,
            Name = "Test Value",
            Value = "test"
        };

        await _dbContext.RangeValues.AddAsync(rangeValue);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetRangeValueByIdAsync(rangeValue.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(rangeValue.Id, result.Id);
        Assert.Equal("Test Value", result.Name);
        Assert.Equal("test", result.Value);
    }

    [Fact]
    public async Task GetRangeValueByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _service.GetRangeValueByIdAsync(-1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateRangeValueAsync_WithValidRangeValue_CreatesRangeValue()
    {
        // Arrange
        var rangeValue = new RangeValue
        {
            RangeId = 1,
            Name = "New Value",
            Value = "new"
        };

        // Act
        var result = await _service.CreateRangeValueAsync(rangeValue);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Value", result.Name);
        Assert.Equal("new", result.Value);
        Assert.True(result.Id > 0);

        var createdRangeValue = await _dbContext.RangeValues.FindAsync(result.Id);
        Assert.NotNull(createdRangeValue);
        Assert.Equal("New Value", createdRangeValue.Name);
    }

    [Fact]
    public async Task CreateRangeValueAsync_WithNullRangeValue_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.CreateRangeValueAsync(null!));
    }

    [Fact]
    public async Task UpdateRangeValueAsync_WithValidRangeValue_UpdatesRangeValue()
    {
        // Arrange
        var existingRangeValue = new RangeValue
        {
            RangeId = 1,
            Name = "Original Value",
            Value = "original"
        };

        await _dbContext.RangeValues.AddAsync(existingRangeValue);
        await _dbContext.SaveChangesAsync();

        var updatedRangeValue = existingRangeValue.Update("Updated Value", "updated");

        // Act
        await _service.UpdateRangeValueAsync(updatedRangeValue);

        // Assert
        var resultRangeValue = await _dbContext.RangeValues.FindAsync(existingRangeValue.Id);
        Assert.NotNull(resultRangeValue);
        Assert.Equal("Updated Value", resultRangeValue.Name);
        Assert.Equal("updated", resultRangeValue.Value);
    }

    [Fact]
    public async Task UpdateRangeValueAsync_WithNullRangeValue_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.UpdateRangeValueAsync(null!));
    }

    [Fact]
    public async Task DeleteRangeValueAsync_WithValidId_DeletesRangeValue()
    {
        // Arrange
        var rangeValue = new RangeValue
        {
            RangeId = 1,
            Name = "Value to Delete",
            Value = "delete"
        };

        await _dbContext.RangeValues.AddAsync(rangeValue);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.DeleteRangeValueAsync(rangeValue.Id);

        // Assert
        var deletedRangeValue = await _dbContext.RangeValues.FindAsync(rangeValue.Id);
        Assert.Null(deletedRangeValue);
    }

    [Fact]
    public async Task DeleteRangeValueAsync_WithInvalidId_ThrowsEntityNotFoundException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _service.DeleteRangeValueAsync(-1));

        Assert.Equal(-1, exception.EntityId);
        Assert.Equal("RangeValue", exception.EntityType);
    }

    [Fact]
    public async Task GetAllRangeValuesAsync_ReturnsAllRangeValues()
    {
        // Arrange
        var rangeValues = new List<RangeValue>
            {
                new() { RangeId = 1, Name = "Value 1", Value = "1" },
                new() { RangeId = 1, Name = "Value 2", Value = "2" }
            };

        await _dbContext.RangeValues.AddRangeAsync(rangeValues);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetAllRangeValuesAsync();

        // Assert
        var valueList = result.ToList();
        Assert.Equal(2, valueList.Count);
        Assert.Contains(valueList, rv => rv.Name == "Value 1");
        Assert.Contains(valueList, rv => rv.Name == "Value 2");
    }

    [Fact]
    public async Task GetRangeValuesForRangeAsync_WithValidRangeId_ReturnsMatchingValues()
    {
        // Arrange
        var rangeId = 1;
        var rangeValues = new List<RangeValue>
            {
                new() { RangeId = rangeId, Name = "Value 1", Value = "1" },
                new() { RangeId = rangeId, Name = "Value 2", Value = "2" },
                new() { RangeId = 2, Name = "Value 3", Value = "3" }
            };

        await _dbContext.RangeValues.AddRangeAsync(rangeValues);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetRangeValuesForRangeAsync(rangeId);

        // Assert
        var valueList = result.ToList();
        Assert.Equal(2, valueList.Count);
        Assert.All(valueList, rv => Assert.Equal(rangeId, rv.RangeId));
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RangeValueService(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RangeValueService(_rangeValueRepository, null!));
    }
}
