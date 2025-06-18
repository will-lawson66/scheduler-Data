namespace Instrument.Scheduling.Data.UT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Instrument.Execution.Parameter;
using Instrument.Scheduling.Data.Initialization;
using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Services.Cleanup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Microsoft.Data.Sqlite;

public class DatabaseCleanupServiceTests : IDisposable
{
    private readonly SchedulerDbContext _dbContext;
    private readonly Mock<ILogger<DatabaseCleanupService>> _mockLogger;
    private readonly DatabaseCleanupService _service;
    private readonly SqliteConnection _connection;
    private readonly SqliteDatabaseInitializer _databaseInitializer;

    public DatabaseCleanupServiceTests()
    {
        //_connection = new SqliteConnection("Filename=:memory:");
        _connection = new SqliteConnection("Data Source=./data/scheduler.db");
        //_connection.Open();

        //_dbName = $"TestDb_{Guid.NewGuid()}";
        //var options = new DbContextOptionsBuilder<SchedulerDbContext>()
        //    .UseInMemoryDatabase(databaseName: _dbName)
        //    .Options;
        var sqliteOptions = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseSqlite(_connection)
            .Options;
        _dbContext = new SchedulerDbContext(sqliteOptions);
        _mockLogger = new Mock<ILogger<DatabaseCleanupService>>();
        _service = new DatabaseCleanupService(_dbContext, _mockLogger.Object);
        _databaseInitializer = new SqliteDatabaseInitializer(_dbContext, new NullLogger<SqliteDatabaseInitializer>());
    }


    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task ClearAllDataAsync_WithEmptyDatabase_CompletesSuccessfully()
    {
        // Act
        await _service.ClearAllDataAsync();

        // Assert - Should complete without throwing
        Assert.True(true); // Test passes if no exception is thrown
    }

    [Fact]
    public async Task ClearAllDataAsync_WithDataInAllTables_ClearsAllData()
    {
        // Arrange - Create test data in all tables
        await InitializeAndSeedTestDataAsync();

        // Verify data exists before cleanup
        Assert.True(await _dbContext.Parameters.AnyAsync());
        Assert.True(await _dbContext.Sequences.AnyAsync());
        Assert.True(await _dbContext.SequenceGroups.AnyAsync());
        Assert.True(await _dbContext.Ranges.AnyAsync());
        Assert.True(await _dbContext.Resources.AnyAsync());
        Assert.True(await _dbContext.RangeValues.AnyAsync());
        Assert.True(await _dbContext.SequenceParameters.AnyAsync());
        Assert.True(await _dbContext.SequenceGroupSequences.AnyAsync());

        // Act
        await _service.ClearAllDataAsync();

        // Assert - All tables should be empty
        Assert.False(await _dbContext.Parameters.AnyAsync());
        Assert.False(await _dbContext.Sequences.AnyAsync());
        Assert.False(await _dbContext.SequenceGroups.AnyAsync());
        Assert.False(await _dbContext.Ranges.AnyAsync());
        Assert.False(await _dbContext.Resources.AnyAsync());
        Assert.False(await _dbContext.RangeValues.AnyAsync());
        Assert.False(await _dbContext.SequenceParameters.AnyAsync());
        Assert.False(await _dbContext.SequenceGroupSequences.AnyAsync());
        Assert.False(await _dbContext.SequenceGroupCollections.AnyAsync());
        Assert.False(await _dbContext.SequenceGroupCollectionSequenceGroups.AnyAsync());
    }

    [Fact]
    public async Task ClearAllDataAsync_WithForeignKeyConstraints_DeletesInCorrectOrder()
    {
        // Arrange - Create data with foreign key relationships
        var range = new Entities.Range { Name = "Test Range", Description = "Test" };
        await _dbContext.Ranges.AddAsync(range);
        await _dbContext.SaveChangesAsync();

        var rangeValue = new RangeValue
        {
            RangeId = range.Id,
            Name = "Test Value",
            Value = "test"
        };
        await _dbContext.RangeValues.AddAsync(rangeValue);

        var resource = new Resource { Name = "Test Resource", Code = "TR001" };
        await _dbContext.Resources.AddAsync(resource);
        await _dbContext.SaveChangesAsync();

        var parameter = new Parameter
        {
            Name = "Test Parameter",
            Type = ParameterType.StringType,
            ResourceId = resource.Id
        };
        await _dbContext.Parameters.AddAsync(parameter);

        var sequence = new Sequence
        {
            Name = "Test Sequence",
            WorstCaseTime = TimeSpan.FromSeconds(30)
        };
        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();

        var sequenceParameter = new SequenceParameter
        {
            SequenceId = sequence.Id,
            ParameterId = parameter.Id,
            OrderNumber = 1
        };
        await _dbContext.SequenceParameters.AddAsync(sequenceParameter);
        await _dbContext.SaveChangesAsync();

        // Act - Should not throw constraint violations
        var exception = await Record.ExceptionAsync(() => _service.ClearAllDataAsync());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task ClearAllDataAsync_LogsCorrectMessages()
    {
        // Arrange
        await InitializeAndSeedTestDataAsync();

        // Act
        await _service.ClearAllDataAsync();

        // Assert - Verify logging calls
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Beginning database cleanup operation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Database cleanup completed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DatabaseCleanupService(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DatabaseCleanupService(_dbContext, null!));
    }

    [Fact]
    public async Task ClearAllDataAsync_WithLargeDataset_ClearsEfficiently()
    {
        // Arrange - Create a larger dataset
        var ranges = Enumerable.Range(1, 100)
            .Select(i => new Entities.Range { Name = $"Range {i}", Description = $"Description {i}" })
            .ToList();
        await _dbContext.Ranges.AddRangeAsync(ranges);
        await _dbContext.SaveChangesAsync();

        var rangeValues = new List<RangeValue>();
        foreach (var range in ranges)
        {
            for (int j = 1; j <= 5; j++)
            {
                rangeValues.Add(new RangeValue
                {
                    RangeId = range.Id,
                    Name = $"Value {j}",
                    Value = j.ToString()
                });
            }
        }
        await _dbContext.RangeValues.AddRangeAsync(rangeValues);
        await _dbContext.SaveChangesAsync();

        // Verify large dataset exists
        Assert.Equal(100, await _dbContext.Ranges.CountAsync());
        Assert.Equal(500, await _dbContext.RangeValues.CountAsync());

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await _service.ClearAllDataAsync();
        stopwatch.Stop();

        // Assert
        Assert.Equal(0, await _dbContext.Ranges.CountAsync());
        Assert.Equal(0, await _dbContext.RangeValues.CountAsync());

        // Should complete in reasonable time (less than 5 seconds for this dataset)
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5),
            $"Cleanup took too long: {stopwatch.Elapsed}");
    }

    private async Task InitializeAndSeedTestDataAsync()
    {
        
        await _databaseInitializer.InitializeAsync();

        // Create test data in all tables to verify complete cleanup
        var range = new Entities.Range { Name = "Test Range", Description = "Test" };
        await _dbContext.Ranges.AddAsync(range);

        var resource = new Resource { Name = "Test Resource", Code = "TR001" };
        await _dbContext.Resources.AddAsync(resource);

        var sequence = new Sequence
        {
            Name = "Test Sequence",
            WorstCaseTime = TimeSpan.FromSeconds(30)
        };
        await _dbContext.Sequences.AddAsync(sequence);

        var sequenceGroup = new SequenceGroup
        {
            Name = "Test Group",
            Description = "Test Group Description"
        };
        await _dbContext.SequenceGroups.AddAsync(sequenceGroup);

        var sequenceGroupCollection = new SequenceGroupCollection<TestEnum>
        {
            Name = "Test Collection",
            Category = TestEnum.CategoryA
        };
        await _dbContext.SequenceGroupCollections.AddAsync(sequenceGroupCollection);

        await _dbContext.SaveChangesAsync();

        // Add child records
        var rangeValue = new RangeValue
        {
            RangeId = range.Id,
            Name = "Test Value",
            Value = "test"
        };
        await _dbContext.RangeValues.AddAsync(rangeValue);

        var parameter = new Parameter
        {
            Name = "Test Parameter",
            Type = ParameterType.StringType,
            ResourceId = resource.Id
        };
        await _dbContext.Parameters.AddAsync(parameter);

        await _dbContext.SaveChangesAsync();

        var sequenceParameter = new SequenceParameter
        {
            SequenceId = sequence.Id,
            ParameterId = parameter.Id,
            OrderNumber = 1
        };
        await _dbContext.SequenceParameters.AddAsync(sequenceParameter);

        var sequenceGroupSequence = new SequenceGroupSequence
        {
            SequenceGroupId = sequenceGroup.Id,
            SequenceId = sequence.Id,
            Order = 1
        };
        await _dbContext.SequenceGroupSequences.AddAsync(sequenceGroupSequence);

        var sequenceGroupCollectionSequenceGroup = new SequenceGroupCollectionSequenceGroup
        {
            SequenceGroupCollectionId = sequenceGroupCollection.Id,
            SequenceGroupId = sequenceGroup.Id,
            Order = 1
        };
        await _dbContext.SequenceGroupCollectionSequenceGroups.AddAsync(sequenceGroupCollectionSequenceGroup);

        await _dbContext.SaveChangesAsync();
    }

    private enum TestEnum
    {
        CategoryA,
        CategoryB
    }
}
