using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;
using Instrument.Data.Exceptions;
using Instrument.Data.Repository;
using Instrument.Data.Services.Cleanup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Instrument.Data.UT.Services
{
    public class DatabaseCleanupServiceTests : IDisposable
    {
        private readonly SchedulerDbContext _dbContext;
        private readonly Mock<ILogger<DatabaseCleanupService>> _mockLogger;
        private readonly DatabaseCleanupService _service;
        private readonly string _dbName;

        public DatabaseCleanupServiceTests()
        {
            _dbName = $"TestDb_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<SchedulerDbContext>()
                .UseInMemoryDatabase(databaseName: _dbName)
                .Options;
            
            _dbContext = new SchedulerDbContext(options);
            _mockLogger = new Mock<ILogger<DatabaseCleanupService>>();
            _service = new DatabaseCleanupService(_dbContext, _mockLogger.Object);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
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
            await SeedTestDataAsync();

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
            await SeedTestDataAsync();

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

        private async Task SeedTestDataAsync()
        {
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

            var sequenceGroupCollectionSequenceGroup = new SequenceGroupCollectionSequenceGroup<TestEnum>
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
}

namespace Instrument.Data.UT.Repository
{
    /// <summary>
    /// Updated ParameterRepositoryTests - the original file was empty
    /// </summary>
    public class ParameterRepositoryTests : IDisposable
    {
        private readonly SchedulerDbContext _dbContext;
        private readonly ParameterRepository _repository;
        private readonly string _dbName;

        public ParameterRepositoryTests()
        {
            _dbName = $"TestDb_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<SchedulerDbContext>()
                .UseInMemoryDatabase(databaseName: _dbName)
                .Options;
            
            _dbContext = new SchedulerDbContext(options);
            _repository = new ParameterRepository(_dbContext);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllParameters()
        {
            // Arrange
            var parameters = new List<Parameter>
            {
                new() { Name = "Parameter 1", Type = ParameterType.StringType },
                new() { Name = "Parameter 2", Type = ParameterType.IntegerType },
                new() { Name = "Parameter 3", Type = ParameterType.BooleanType }
            };

            await _dbContext.Parameters.AddRangeAsync(parameters);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            var parameterList = result.ToList();
            Assert.Equal(3, parameterList.Count);
            Assert.Contains(parameterList, p => p.Name == "Parameter 1");
            Assert.Contains(parameterList, p => p.Name == "Parameter 2");
            Assert.Contains(parameterList, p => p.Name == "Parameter 3");
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsParameter()
        {
            // Arrange
            var parameter = new Parameter 
            { 
                Name = "Test Parameter", 
                Type = ParameterType.StringType,
                Description = "Test Description",
                Min = "0",
                Max = "100"
            };

            await _dbContext.Parameters.AddAsync(parameter);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(parameter.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(parameter.Id, result.Id);
            Assert.Equal("Test Parameter", result.Name);
            Assert.Equal(ParameterType.StringType, result.Type);
            Assert.Equal("Test Description", result.Description);
            Assert.Equal("0", result.Min);
            Assert.Equal("100", result.Max);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(-1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetQueryableAsync_ReturnsQueryable()
        {
            // Arrange
            var parameters = new List<Parameter>
            {
                new() { Name = "Parameter 1", Type = ParameterType.StringType },
                new() { Name = "Parameter 2", Type = ParameterType.IntegerType }
            };

            await _dbContext.Parameters.AddRangeAsync(parameters);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetQueryableAsync();

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IQueryable<Parameter>>(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task AddAsync_AddsParameter()
        {
            // Arrange
            var parameter = new Parameter 
            { 
                Name = "New Parameter", 
                Type = ParameterType.StringType,
                Description = "New Description"
            };

            // Act
            await _repository.AddAsync(parameter);
            await _repository.SaveChangesAsync();

            // Assert
            var result = await _dbContext.Parameters.FindAsync(parameter.Id);
            Assert.NotNull(result);
            Assert.Equal("New Parameter", result.Name);
            Assert.Equal(ParameterType.StringType, result.Type);
            Assert.Equal("New Description", result.Description);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesParameter()
        {
            // Arrange
            var original = new Parameter 
            { 
                Name = "Original Parameter", 
                Type = ParameterType.StringType,
                Description = "Original Description"
            };

            await _dbContext.Parameters.AddAsync(original);
            await _dbContext.SaveChangesAsync();

            var updated = original.Update("Updated Parameter", ParameterType.IntegerType);
            updated.Description = "Updated Description";

            // Act
            await _repository.UpdateAsync(updated);
            await _repository.SaveChangesAsync();

            // Assert
            var result = await _dbContext.Parameters.FindAsync(updated.Id);
            Assert.NotNull(result);
            Assert.Equal("Updated Parameter", result.Name);
            Assert.Equal(ParameterType.IntegerType, result.Type);
            Assert.Equal("Updated Description", result.Description);
        }

        [Fact]
        public async Task DeleteAsync_DeletesParameter()
        {
            // Arrange
            var parameter = new Parameter 
            { 
                Name = "Delete Parameter", 
                Type = ParameterType.StringType
            };

            await _dbContext.Parameters.AddAsync(parameter);
            await _dbContext.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(parameter.Id);
            await _repository.SaveChangesAsync();

            // Assert
            var result = await _dbContext.Parameters.FindAsync(parameter.Id);
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ThrowsEntityNotFoundException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _repository.DeleteAsync(-1));
        }

        [Fact]
        public async Task GetParametersByType_ReturnsCorrectParameters()
        {
            // Arrange
            var parameters = new List<Parameter>
            {
                new() { Name = "String Param 1", Type = ParameterType.StringType },
                new() { Name = "String Param 2", Type = ParameterType.StringType },
                new() { Name = "Int Param 1", Type = ParameterType.IntegerType },
                new() { Name = "Bool Param 1", Type = ParameterType.BooleanType }
            };

            await _dbContext.Parameters.AddRangeAsync(parameters);
            await _dbContext.SaveChangesAsync();

            // Act
            var queryable = await _repository.GetQueryableAsync();
            var stringParameters = queryable.Where(p => p.Type == ParameterType.StringType).ToList();

            // Assert
            Assert.Equal(2, stringParameters.Count);
            Assert.All(stringParameters, p => Assert.Equal(ParameterType.StringType, p.Type));
        }

        [Fact]
        public async Task GetParametersWithResource_ReturnsParametersWithResourceInfo()
        {
            // Arrange
            var resource = new Resource { Name = "Test Resource", Code = "TR001" };
            await _dbContext.Resources.AddAsync(resource);
            await _dbContext.SaveChangesAsync();

            var parameters = new List<Parameter>
            {
                new() { Name = "Param 1", Type = ParameterType.StringType, ResourceId = resource.Id },
                new() { Name = "Param 2", Type = ParameterType.IntegerType, ResourceId = resource.Id },
                new() { Name = "Param 3", Type = ParameterType.BooleanType } // No resource
            };

            await _dbContext.Parameters.AddRangeAsync(parameters);
            await _dbContext.SaveChangesAsync();

            // Act
            var queryable = await _repository.GetQueryableAsync();
            var parametersWithResource = queryable.Where(p => p.ResourceId == resource.Id).ToList();

            // Assert
            Assert.Equal(2, parametersWithResource.Count);
            Assert.All(parametersWithResource, p => Assert.Equal(resource.Id, p.ResourceId));
        }

        [Fact]
        public async Task GetParametersWithValidation_ReturnsParametersWithMinMax()
        {
            // Arrange
            var parameters = new List<Parameter>
            {
                new() 
                { 
                    Name = "Validated Param", 
                    Type = ParameterType.IntegerType,
                    Min = "0",
                    Max = "100"
                },
                new() 
                { 
                    Name = "Unvalidated Param", 
                    Type = ParameterType.StringType
                }
            };

            await _dbContext.Parameters.AddRangeAsync(parameters);
            await _dbContext.SaveChangesAsync();

            // Act
            var queryable = await _repository.GetQueryableAsync();
            var validatedParameters = queryable.Where(p => p.Min != null && p.Max != null).ToList();

            // Assert
            Assert.Single(validatedParameters);
            Assert.Equal("Validated Param", validatedParameters.First().Name);
            Assert.Equal("0", validatedParameters.First().Min);
            Assert.Equal("100", validatedParameters.First().Max);
        }

        [Fact]
        public async Task SaveChangesAsync_PersistsChanges()
        {
            // Arrange
            var parameter = new Parameter 
            { 
                Name = "Test Parameter", 
                Type = ParameterType.StringType
            };

            await _repository.AddAsync(parameter);

            // Act
            await _repository.SaveChangesAsync();

            // Assert
            var result = await _dbContext.Parameters.FindAsync(parameter.Id);
            Assert.NotNull(result);
            Assert.Equal("Test Parameter", result.Name);
        }

        [Fact]
        public async Task ConcurrentAccess_HandlesCorrectly()
        {
            // Arrange
            var tasks = new List<Task>();
            
            // Act - Create multiple concurrent operations
            for (int i = 0; i < 10; i++)
            {
                var parameterIndex = i;
                tasks.Add(Task.Run(async () =>
                {
                    var parameter = new Parameter 
                    { 
                        Name = $"Concurrent Parameter {parameterIndex}", 
                        Type = ParameterType.StringType
                    };
                    await _repository.AddAsync(parameter);
                    await _repository.SaveChangesAsync();
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            var allParameters = await _repository.GetAllAsync();
            Assert.Equal(10, allParameters.Count());
        }
    }
}