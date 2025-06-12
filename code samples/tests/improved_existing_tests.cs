using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;
using Instrument.Data.Exceptions;
using Instrument.Data.Repository;
using Instrument.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Instrument.Data.UT.Services
{
    /// <summary>
    /// Improved SequenceServiceTests with additional edge cases and validation
    /// </summary>
    public class SequenceServiceImprovedTests : IDisposable
    {
        private readonly SchedulerDbContext _dbContext;
        private readonly ISequenceRepository _sequenceRepository;
        private readonly Mock<ILogger<SequenceService>> _mockLogger;
        private readonly SequenceService _service;
        private readonly string _dbName;

        public SequenceServiceImprovedTests()
        {
            _dbName = $"TestDb_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<SchedulerDbContext>()
                .UseInMemoryDatabase(databaseName: _dbName)
                .Options;
            
            _dbContext = new SchedulerDbContext(options);
            _sequenceRepository = new SequenceRepository(_dbContext);
            _mockLogger = new Mock<ILogger<SequenceService>>();
            _service = new SequenceService(_sequenceRepository, _mockLogger.Object);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Fact]
        public async Task AddParameterToSequenceAsync_WithNonExistentSequence_ThrowsEntityNotFoundException()
        {
            // Arrange
            var parameter = new Parameter { Name = "Test Parameter", Type = ParameterType.StringType };
            await _dbContext.Parameters.AddAsync(parameter);
            await _dbContext.SaveChangesAsync();

            var nonExistentSequenceId = -1;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _service.AddParameterToSequenceAsync(parameter.Id, nonExistentSequenceId, 1));

            Assert.Equal(nonExistentSequenceId, exception.EntityId);
            Assert.Equal("Sequence", exception.EntityType);
        }

        [Fact]
        public async Task CreateSequenceAsync_WithNullSequence_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.CreateSequenceAsync(null!));
        }

        [Fact]
        public async Task UpdateSequenceAsync_WithNullSequence_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.UpdateSequenceAsync(null!));
        }

        [Fact]
        public async Task SearchSequencesAsync_WithNullPredicate_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.SearchSequencesAsync(null!));
        }

        [Fact]
        public async Task GetSequenceWithParametersAsync_WithValidId_ReturnsSequenceWithOrderedParameters()
        {
            // Arrange
            var sequence = new Sequence { Name = "Test Sequence", WorstCaseTime = TimeSpan.FromSeconds(30) };
            await _dbContext.Sequences.AddAsync(sequence);

            var param1 = new Parameter { Name = "Parameter 1", Type = ParameterType.StringType };
            var param2 = new Parameter { Name = "Parameter 2", Type = ParameterType.IntegerType };
            var param3 = new Parameter { Name = "Parameter 3", Type = ParameterType.BooleanType };
            
            await _dbContext.Parameters.AddRangeAsync(param1, param2, param3);
            await _dbContext.SaveChangesAsync();

            // Add parameters in non-sequential order
            await _dbContext.SequenceParameters.AddRangeAsync(
                new SequenceParameter { SequenceId = sequence.Id, ParameterId = param2.Id, OrderNumber = 2 },
                new SequenceParameter { SequenceId = sequence.Id, ParameterId = param1.Id, OrderNumber = 1 },
                new SequenceParameter { SequenceId = sequence.Id, ParameterId = param3.Id, OrderNumber = 3 }
            );
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetSequenceWithParametersAsync(sequence.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.SequenceParameters.Count);
            
            var orderedParams = result.SequenceParameters.OrderBy(sp => sp.OrderNumber).ToList();
            Assert.Equal(param1.Id, orderedParams[0].ParameterId);
            Assert.Equal(param2.Id, orderedParams[1].ParameterId);
            Assert.Equal(param3.Id, orderedParams[2].ParameterId);
        }

        [Fact]
        public async Task RemoveParameterFromSequenceAsync_WithValidIds_RemovesAssociation()
        {
            // Arrange
            var sequence = new Sequence { Name = "Test Sequence", WorstCaseTime = TimeSpan.FromSeconds(30) };
            var parameter = new Parameter { Name = "Test Parameter", Type = ParameterType.StringType };
            
            await _dbContext.Sequences.AddAsync(sequence);
            await _dbContext.Parameters.AddAsync(parameter);
            await _dbContext.SaveChangesAsync();

            var sequenceParameter = new SequenceParameter
            {
                SequenceId = sequence.Id,
                ParameterId = parameter.Id,
                OrderNumber = 1
            };
            await _dbContext.SequenceParameters.AddAsync(sequenceParameter);
            await _dbContext.SaveChangesAsync();

            // Act
            await _service.RemoveParameterFromSequenceAsync(parameter.Id, sequence.Id);

            // Assert
            var association = await _dbContext.SequenceParameters
                .FirstOrDefaultAsync(sp => sp.SequenceId == sequence.Id && sp.ParameterId == parameter.Id);
            Assert.Null(association);
        }

        [Fact]
        public async Task SearchSequencesAsync_WithComplexPredicate_ReturnsMatchingSequences()
        {
            // Arrange
            var sequences = new List<Sequence>
            {
                new() { Name = "Fast Sequence", WorstCaseTime = TimeSpan.FromSeconds(10), CanBeParallel = true },
                new() { Name = "Slow Sequence", WorstCaseTime = TimeSpan.FromSeconds(60), CanBeParallel = false },
                new() { Name = "Medium Sequence", WorstCaseTime = TimeSpan.FromSeconds(30), CanBeParallel = true }
            };

            await _dbContext.Sequences.AddRangeAsync(sequences);
            await _dbContext.SaveChangesAsync();

            // Act - Find parallel sequences that take less than 45 seconds
            var result = await _service.SearchSequencesAsync(s => 
                s.CanBeParallel && s.WorstCaseTime < TimeSpan.FromSeconds(45));

            // Assert
            var resultList = result.ToList();
            Assert.Equal(2, resultList.Count);
            Assert.Contains(resultList, s => s.Name == "Fast Sequence");
            Assert.Contains(resultList, s => s.Name == "Medium Sequence");
        }

        [Fact]
        public void Constructor_WithNullRepository_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new SequenceService(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new SequenceService(_sequenceRepository, null!));
        }
    }

    /// <summary>
    /// Tests for SequenceGroupCollectionService which was missing
    /// </summary>
    public class SequenceGroupCollectionServiceTests : IDisposable
    {
        private readonly SchedulerDbContext _dbContext;
        private readonly ISequenceGroupCollectionRepository<TestEnum> _collectionRepository;
        private readonly ISequenceGroupRepository _sequenceGroupRepository;
        private readonly Mock<ILogger<SequenceGroupCollectionService<TestEnum>>> _mockLogger;
        private readonly SequenceGroupCollectionService<TestEnum> _service;
        private readonly string _dbName;

        public SequenceGroupCollectionServiceTests()
        {
            _dbName = $"TestDb_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<SchedulerDbContext>()
                .UseInMemoryDatabase(databaseName: _dbName)
                .Options;
            
            _dbContext = new SchedulerDbContext(options);
            _collectionRepository = new SequenceGroupCollectionRepository<TestEnum>(_dbContext);
            _sequenceGroupRepository = new SequenceGroupRepository(_dbContext);
            _mockLogger = new Mock<ILogger<SequenceGroupCollectionService<TestEnum>>>();
            
            _service = new SequenceGroupCollectionService<TestEnum>(
                _collectionRepository,
                _sequenceGroupRepository,
                _mockLogger.Object
            );
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Fact]
        public async Task CreateSequenceGroupCollectionAsync_WithValidData_CreatesCollection()
        {
            // Arrange
            var collection = new SequenceGroupCollection<TestEnum>
            {
                Name = "Test Collection",
                Description = "Test Description",
                Category = TestEnum.CategoryA
            };

            // Act
            var result = await _service.CreateSequenceGroupCollectionAsync(collection);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Collection", result.Name);
            Assert.Equal("Test Description", result.Description);
            Assert.Equal(TestEnum.CategoryA, result.Category);
            Assert.True(result.Id > 0);

            var savedCollection = await _dbContext.SequenceGroupCollections.FindAsync(result.Id);
            Assert.NotNull(savedCollection);
        }

        [Fact]
        public async Task CreateSequenceGroupCollectionAsync_WithEmptyName_ThrowsArgumentException()
        {
            // Arrange
            var collection = new SequenceGroupCollection<TestEnum>
            {
                Name = "",
                Category = TestEnum.CategoryA
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.CreateSequenceGroupCollectionAsync(collection));
        }

        [Fact]
        public async Task CreateSequenceGroupCollectionAsync_WithNullName_ThrowsArgumentException()
        {
            // Arrange
            var collection = new SequenceGroupCollection<TestEnum>
            {
                Name = null!,
                Category = TestEnum.CategoryA
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.CreateSequenceGroupCollectionAsync(collection));
        }

        [Fact]
        public async Task UpdateSequenceGroupCollectionAsync_WithValidData_UpdatesCollection()
        {
            // Arrange
            var collection = new SequenceGroupCollection<TestEnum>
            {
                Name = "Original Collection",
                Description = "Original Description",
                Category = TestEnum.CategoryA
            };

            await _dbContext.SequenceGroupCollections.AddAsync(collection);
            await _dbContext.SaveChangesAsync();

            collection.Name = "Updated Collection";
            collection.Description = "Updated Description";
            collection.Category = TestEnum.CategoryB;

            // Act
            await _service.UpdateSequenceGroupCollectionAsync(collection);

            // Assert
            var updatedCollection = await _dbContext.SequenceGroupCollections.FindAsync(collection.Id);
            Assert.NotNull(updatedCollection);
            Assert.Equal("Updated Collection", updatedCollection.Name);
            Assert.Equal("Updated Description", updatedCollection.Description);
            Assert.Equal(TestEnum.CategoryB, updatedCollection.Category);
        }

        [Fact]
        public async Task UpdateSequenceGroupCollectionAsync_WithNonExistentId_ThrowsEntityNotFoundException()
        {
            // Arrange
            var collection = new SequenceGroupCollection<TestEnum>
            {
                Id = -1,
                Name = "Non-existent Collection",
                Category = TestEnum.CategoryA
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
                _service.UpdateSequenceGroupCollectionAsync(collection));

            Assert.Equal(-1, exception.EntityId);
            Assert.Equal("SequenceGroupCollection", exception.EntityType);
        }

        [Fact]
        public async Task UpdateSequenceGroupCollectionAsync_WithNullCollection_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.UpdateSequenceGroupCollectionAsync(null!));
        }

        [Fact]
        public async Task DeleteSequenceGroupCollectionAsync_WithValidId_DeletesCollection()
        {
            // Arrange
            var collection = new SequenceGroupCollection<TestEnum>
            {
                Name = "Collection to Delete",
                Category = TestEnum.CategoryA
            };

            await _dbContext.SequenceGroupCollections.AddAsync(collection);
            await _dbContext.SaveChangesAsync();

            // Act
            await _service.DeleteSequenceGroupCollectionAsync(collection.Id);

            // Assert
            var deletedCollection = await _dbContext.SequenceGroupCollections.FindAsync(collection.Id);
            Assert.Null(deletedCollection);
        }

        [Fact]
        public async Task DeleteSequenceGroupCollectionAsync_WithNonExistentId_ThrowsEntityNotFoundException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
                _service.DeleteSequenceGroupCollectionAsync(-1));

            Assert.Equal(-1, exception.EntityId);
            Assert.Equal("SequenceGroupCollection", exception.EntityType);
        }

        [Fact]
        public async Task GetSequenceGroupCollectionByIdAsync_WithValidId_ReturnsCollection()
        {
            // Arrange
            var collection = new SequenceGroupCollection<TestEnum>
            {
                Name = "Test Collection",
                Category = TestEnum.CategoryA
            };

            await _dbContext.SequenceGroupCollections.AddAsync(collection);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetSequenceGroupCollectionByIdAsync(collection.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Collection", result.Name);
            Assert.Equal(TestEnum.CategoryA, result.Category);
        }

        [Fact]
        public async Task GetSequenceGroupCollectionByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _service.GetSequenceGroupCollectionByIdAsync(-1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllSequenceGroupCollectionsAsync_ReturnsAllCollections()
        {
            // Arrange
            var collections = new List<SequenceGroupCollection<TestEnum>>
            {
                new() { Name = "Collection 1", Category = TestEnum.CategoryA },
                new() { Name = "Collection 2", Category = TestEnum.CategoryB }
            };

            await _dbContext.SequenceGroupCollections.AddRangeAsync(collections);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetAllSequenceGroupCollectionsAsync();

            // Assert
            var collectionList = result.ToList();
            Assert.Equal(2, collectionList.Count);
            Assert.Contains(collectionList, c => c.Name == "Collection 1");
            Assert.Contains(collectionList, c => c.Name == "Collection 2");
        }

        [Fact]
        public async Task GetSequenceGroupCollectionsByCategoryAsync_ReturnsFilteredCollections()
        {
            // Arrange
            var collections = new List<SequenceGroupCollection<TestEnum>>
            {
                new() { Name = "Collection A1", Category = TestEnum.CategoryA },
                new() { Name = "Collection A2", Category = TestEnum.CategoryA },
                new() { Name = "Collection B1", Category = TestEnum.CategoryB }
            };

            await _dbContext.SequenceGroupCollections.AddRangeAsync(collections);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetSequenceGroupCollectionsByCategoryAsync(TestEnum.CategoryA);

            // Assert
            var collectionList = result.ToList();
            Assert.Equal(2, collectionList.Count);
            Assert.All(collectionList, c => Assert.Equal(TestEnum.CategoryA, c.Category));
        }

        [Fact]
        public async Task AddSequenceGroupToSequenceGroupCollectionAsync_WithValidIds_AddsAssociation()
        {
            // Arrange
            var collection = new SequenceGroupCollection<TestEnum>
            {
                Name = "Test Collection",
                Category = TestEnum.CategoryA
            };
            var sequenceGroup = new SequenceGroup { Name = "Test Group" };

            await _dbContext.SequenceGroupCollections.AddAsync(collection);
            await _dbContext.SequenceGroups.AddAsync(sequenceGroup);
            await _dbContext.SaveChangesAsync();

            var order = 1;

            // Act
            var result = await _service.AddSequenceGroupToSequenceGroupCollectionAsync(
                collection.Id, sequenceGroup.Id, order);

            // Assert
            Assert.True(result);

            var association = await _dbContext.SequenceGroupCollectionSequenceGroups
                .FirstOrDefaultAsync(sgc => sgc.SequenceGroupCollectionId == collection.Id 
                                         && sgc.SequenceGroupId == sequenceGroup.Id);
            Assert.NotNull(association);
            Assert.Equal(order, association.Order);
        }

        [Fact]
        public async Task AddSequenceGroupToSequenceGroupCollectionAsync_WithInvalidCollectionId_ReturnsFalse()
        {
            // Arrange
            var sequenceGroup = new SequenceGroup { Name = "Test Group" };
            await _dbContext.SequenceGroups.AddAsync(sequenceGroup);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.AddSequenceGroupToSequenceGroupCollectionAsync(
                -1, sequenceGroup.Id, 1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AddSequenceGroupToSequenceGroupCollectionAsync_WithInvalidSequenceGroupId_ReturnsFalse()
        {
            // Arrange
            var collection = new SequenceGroupCollection<TestEnum>
            {
                Name = "Test Collection",
                Category = TestEnum.CategoryA
            };
            await _dbContext.SequenceGroupCollections.AddAsync(collection);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.AddSequenceGroupToSequenceGroupCollectionAsync(
                collection.Id, -1, 1);

            // Assert
            Assert.False(result);
        }

        private enum TestEnum
        {
            CategoryA,
            CategoryB
        }
    }

    /// <summary>
    /// Enhanced validation tests for ParameterService
    /// </summary>
    public class ParameterServiceValidationTests : IDisposable
    {
        private readonly SchedulerDbContext _dbContext;
        private readonly IParameterRepository _parameterRepository;
        private readonly Mock<ILogger<ParameterService>> _mockLogger;
        private readonly ParameterService _service;

        public ParameterServiceValidationTests()
        {
            var dbName = $"TestDb_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<SchedulerDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            
            _dbContext = new SchedulerDbContext(options);
            _parameterRepository = new ParameterRepository(_dbContext);
            _mockLogger = new Mock<ILogger<ParameterService>>();
            _service = new ParameterService(_parameterRepository, _mockLogger.Object);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Theory]
        [InlineData(ParameterType.IntegerType, "50", "0", "100", true)]
        [InlineData(ParameterType.IntegerType, "150", "0", "100", false)]
        [InlineData(ParameterType.IntegerType, "-10", "0", "100", false)]
        [InlineData(ParameterType.StringType, "Hello", "3", "10", true)]
        [InlineData(ParameterType.StringType, "Hi", "3", "10", false)]
        [InlineData(ParameterType.StringType, "This is too long", "3", "10", false)]
        [InlineData(ParameterType.BooleanType, "true", null, null, true)]
        [InlineData(ParameterType.BooleanType, "false", null, null, true)]
        [InlineData(ParameterType.BooleanType, "maybe", null, null, false)]
        public void ValidateParameterValue_WithVariousInputs_ReturnsExpectedResult(
            ParameterType type, string value, string? min, string? max, bool expectedValid)
        {
            // Arrange
            var parameter = new Parameter
            {
                Name = "Test Parameter",
                Type = type,
                Min = min,
                Max = max
            };

            // Act & Assert
            if (expectedValid)
            {
                var exception = Record.Exception(() => 
                    _service.ValidateParameterValue(parameter, value));
                Assert.Null(exception);
            }
            else
            {
                Assert.Throws<ValidationException>(() => 
                    _service.ValidateParameterValue(parameter, value));
            }
        }

        [Theory]
        [InlineData(ParameterType.IntegerType, "50", "0", "100", true)]
        [InlineData(ParameterType.IntegerType, "150", "0", "100", false)]
        [InlineData(ParameterType.IntegerType, "not_a_number", "0", "100", false)]
        [InlineData(ParameterType.StringType, "Hello", "3", "10", true)]
        [InlineData(ParameterType.StringType, "Hi", "3", "10", false)]
        [InlineData(ParameterType.BooleanType, "true", null, null, true)]
        [InlineData(ParameterType.BooleanType, "invalid", null, null, false)]
        public void TryValidateParameterValue_WithVariousInputs_ReturnsExpectedResult(
            ParameterType type, string value, string? min, string? max, bool expectedValid)
        {
            // Arrange
            var parameter = new Parameter
            {
                Name = "Test Parameter",
                Type = type,
                Min = min,
                Max = max
            };

            // Act
            var result = _service.TryValidateParameterValue(parameter, value);

            // Assert
            Assert.Equal(expectedValid, result);
        }

        [Fact]
        public void ValidateParameterValue_WithNullValue_HandlesDifferentParameterTypes()
        {
            // Arrange
            var stringParameter = new Parameter
            {
                Name = "String Parameter",
                Type = ParameterType.StringType
            };

            var intParameter = new Parameter
            {
                Name = "Int Parameter",
                Type = ParameterType.IntegerType
            };

            // Act & Assert - null values should be handled gracefully
            var stringException = Record.Exception(() => 
                _service.ValidateParameterValue(stringParameter, null!));
            Assert.NotNull(stringException);

            var intException = Record.Exception(() => 
                _service.ValidateParameterValue(intParameter, null!));
            Assert.NotNull(intException);
        }

        [Fact]
        public void TryValidateParameterValue_WithNullParameter_ReturnsFalse()
        {
            // Act
            var result = _service.TryValidateParameterValue(null!, "test");

            // Assert
            Assert.False(result);
        }
    }
}