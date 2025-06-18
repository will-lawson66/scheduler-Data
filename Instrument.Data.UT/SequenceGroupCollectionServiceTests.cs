//namespace Instrument.Scheduling.Data.UT;

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Instrument.Scheduling.Data;
//using Instrument.Scheduling.Data.DataContext;
//using Instrument.Scheduling.Data.Entities;
//using Instrument.Scheduling.Data.Exceptions;
//using Instrument.Scheduling.Data.Repository;
//using Instrument.Scheduling.Data.Services;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging.Abstractions;

//public class SequenceGroupCollectionServiceTests : IDisposable
//{
//    private readonly SchedulerDbContext _dbContext;
//    private readonly ISequenceGroupCollectionRepository<TestEnum> _collectionRepository;
//    private readonly ISequenceGroupRepository _sequenceGroupRepository;
//    //private readonly Mock<ILogger<SequenceGroupCollectionService<TestEnum>>> _mockLogger;
//    private readonly SequenceGroupCollectionService<TestEnum> _service;
//    private readonly string _dbName;

//    public SequenceGroupCollectionServiceTests()
//    {
//        _dbName = $"TestDb_{Guid.NewGuid()}";
//        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
//            .UseInMemoryDatabase(databaseName: _dbName)
//            .Options;

//        _dbContext = new SchedulerDbContext(options);
//        _collectionRepository = new SequenceGroupCollectionRepository<TestEnum>(_dbContext);
//        _sequenceGroupRepository = new SequenceGroupRepository(_dbContext);
//        //_mockLogger = new Mock<ILogger<SequenceGroupCollectionService<It.IsAnyType>>>();

//        _service = new SequenceGroupCollectionService<TestEnum>(
//            _collectionRepository,
//            _sequenceGroupRepository,
//            NullLogger<SequenceGroupCollectionService<TestEnum>>.Instance
//        );
//    }

//    public void Dispose()
//    {
//        _dbContext.Database.EnsureDeleted();
//        _dbContext.Dispose();
//    }

//    [Fact]
//    public async Task CreateSequenceGroupCollectionAsync_WithValidData_CreatesCollection()
//    {
//        // Arrange
//        var collection = new SequenceGroupCollection<TestEnum>
//        {
//            Name = "Test Collection",
//            Description = "Test Description",
//            Category = TestEnum.CategoryA
//        };

//        // Act
//        var result = await _service.CreateSequenceGroupCollectionAsync(collection);

//        // Assert
//        Assert.NotNull(result);
//        Assert.Equal("Test Collection", result.Name);
//        Assert.Equal("Test Description", result.Description);
//        Assert.Equal(TestEnum.CategoryA, result.Category);
//        Assert.True(result.Id > 0);

//        var savedCollection = await _dbContext.SequenceGroupCollections.FindAsync(result.Id);
//        Assert.NotNull(savedCollection);
//    }

//    [Fact]
//    public async Task CreateSequenceGroupCollectionAsync_WithEmptyName_ThrowsArgumentException()
//    {
//        // Arrange
//        var collection = new SequenceGroupCollection<TestEnum>
//        {
//            Name = "",
//            Category = TestEnum.CategoryA
//        };

//        // Act & Assert
//        await Assert.ThrowsAsync<ArgumentException>(() =>
//            _service.CreateSequenceGroupCollectionAsync(collection));
//    }

//    [Fact]
//    public async Task CreateSequenceGroupCollectionAsync_WithNullName_ThrowsArgumentException()
//    {
//        // Arrange
//        var collection = new SequenceGroupCollection<TestEnum>
//        {
//            Name = null!,
//            Category = TestEnum.CategoryA
//        };

//        // Act & Assert
//        await Assert.ThrowsAsync<ArgumentException>(() =>
//            _service.CreateSequenceGroupCollectionAsync(collection));
//    }

//    [Fact]
//    public async Task UpdateSequenceGroupCollectionAsync_WithValidData_UpdatesCollection()
//    {
//        // Arrange
//        var collection = new SequenceGroupCollection<TestEnum>
//        {
//            Name = "Original Collection",
//            Description = "Original Description",
//            Category = TestEnum.CategoryA,
//        };

//        await _dbContext.SequenceGroupCollections.AddAsync(collection);
//        await _dbContext.SaveChangesAsync();

//        var newCollection = new SequenceGroupCollection<TestEnum>
//        {
//            Id = collection.Id,
//            Name = "Updated Collection",
//            Description = "Updated Description",
//            Category = TestEnum.CategoryB,
//        };
       

//        // Act
//        await _service.UpdateSequenceGroupCollectionAsync(newCollection);

//        // Assert
//        var updatedCollection = await _dbContext.SequenceGroupCollections.FindAsync(collection.Id);
//        Assert.NotNull(updatedCollection);
//        Assert.Equal("Updated Collection", updatedCollection.Name);
//        Assert.Equal("Updated Description", updatedCollection.Description);
//        Assert.Equal(TestEnum.CategoryB.ToString(), updatedCollection.CategoryName);
//    }

//    [Fact]
//    public async Task UpdateSequenceGroupCollectionAsync_WithNonExistentId_ThrowsEntityNotFoundException()
//    {
//        // Arrange
//        var collection = new SequenceGroupCollection<TestEnum>
//        {
//            Id = -1,
//            Name = "Non-existent Collection",
//            Category = TestEnum.CategoryA
//        };

//        // Act & Assert
//        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() =>
//            _service.UpdateSequenceGroupCollectionAsync(collection));

//        Assert.Equal(-1, exception.EntityId);
//        Assert.Equal("SequenceGroupCollection", exception.EntityType);
//    }

//    [Fact]
//    public async Task UpdateSequenceGroupCollectionAsync_WithNullCollection_ThrowsArgumentNullException()
//    {
//        // Act & Assert
//        await Assert.ThrowsAsync<ArgumentNullException>(() =>
//            _service.UpdateSequenceGroupCollectionAsync(null!));
//    }

//    [Fact]
//    public async Task DeleteSequenceGroupCollectionAsync_WithValidId_DeletesCollection()
//    {
//        // Arrange
//        var collection = new SequenceGroupCollection<TestEnum>
//        {
//            Name = "Collection to Delete",
//            Category = TestEnum.CategoryA
//        };

//        await _dbContext.SequenceGroupCollections.AddAsync(collection);
//        await _dbContext.SaveChangesAsync();

//        // Act
//        await _service.DeleteSequenceGroupCollectionAsync(collection.Id);

//        // Assert
//        var deletedCollection = await _dbContext.SequenceGroupCollections.FindAsync(collection.Id);
//        Assert.Null(deletedCollection);
//    }

//    [Fact]
//    public async Task DeleteSequenceGroupCollectionAsync_WithNonExistentId_ThrowsEntityNotFoundException()
//    {
//        // Act & Assert
//        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() =>
//            _service.DeleteSequenceGroupCollectionAsync(-1));

//        Assert.Equal(-1, exception.EntityId);
//        Assert.Equal("SequenceGroupCollection", exception.EntityType);
//    }

//    [Fact]
//    public async Task GetSequenceGroupCollectionByIdAsync_WithValidId_ReturnsCollection()
//    {
//        // Arrange
//        var collection = new SequenceGroupCollection<TestEnum>
//        {
//            Name = "Test Collection",
//            Category = TestEnum.CategoryA
//        };

//        await _dbContext.SequenceGroupCollections.AddAsync(collection);
//        await _dbContext.SaveChangesAsync();

//        // Act
//        var result = await _service.GetSequenceGroupCollectionByIdAsync(collection.Id);

//        // Assert
//        Assert.NotNull(result);
//        Assert.Equal("Test Collection", result.Name);
//        Assert.Equal(TestEnum.CategoryA, result.Category);
//    }

//    [Fact]
//    public async Task GetSequenceGroupCollectionByIdAsync_WithInvalidId_ReturnsNull()
//    {
//        // Act
//        var result = await _service.GetSequenceGroupCollectionByIdAsync(-1);

//        // Assert
//        Assert.Null(result);
//    }

//    [Fact]
//    public async Task GetAllSequenceGroupCollectionsAsync_ReturnsAllCollections()
//    {
//        // Arrange
//        var collections = new List<SequenceGroupCollection<TestEnum>>
//            {
//                new() { Name = "Collection 1", Category = TestEnum.CategoryA },
//                new() { Name = "Collection 2", Category = TestEnum.CategoryB }
//            };

//        await _dbContext.SequenceGroupCollections.AddRangeAsync(collections);
//        await _dbContext.SaveChangesAsync();

//        // Act
//        var result = await _service.GetAllSequenceGroupCollectionsAsync();

//        // Assert
//        var collectionList = result.ToList();
//        Assert.Equal(2, collectionList.Count);
//        Assert.Contains(collectionList, c => c.Name == "Collection 1");
//        Assert.Contains(collectionList, c => c.Name == "Collection 2");
//    }

//    [Fact]
//    public async Task GetSequenceGroupCollectionsByCategoryAsync_ReturnsFilteredCollections()
//    {
//        // Arrange
//        var collections = new List<SequenceGroupCollection<TestEnum>>
//            {
//                new() { Name = "Collection A1", Category = TestEnum.CategoryA },
//                new() { Name = "Collection A2", Category = TestEnum.CategoryA },
//                new() { Name = "Collection B1", Category = TestEnum.CategoryB }
//            };

//        await _dbContext.SequenceGroupCollections.AddRangeAsync(collections);
//        await _dbContext.SaveChangesAsync();

//        // Act
//        var result = await _service.GetSequenceGroupCollectionsByCategoryAsync(TestEnum.CategoryA);

//        // Assert
//        var collectionList = result.ToList();
//        Assert.Equal(2, collectionList.Count);
//        Assert.All(collectionList, c => Assert.Equal(TestEnum.CategoryA, c.Category));
//    }

//    [Fact]
//    public async Task AddSequenceGroupToSequenceGroupCollectionAsync_WithValidIds_AddsAssociation()
//    {
//        // Arrange
//        var collection = new SequenceGroupCollection<TestEnum>
//        {
//            Name = "Test Collection",
//            Category = TestEnum.CategoryA
//        };
//        var sequenceGroup = new SequenceGroup { Name = "Test Group" };

//        await _dbContext.SequenceGroupCollections.AddAsync(collection);
//        await _dbContext.SequenceGroups.AddAsync(sequenceGroup);
//        await _dbContext.SaveChangesAsync();

//        var order = 1;

//        // Act
//        var result = await _service.AddSequenceGroupToSequenceGroupCollectionAsync(
//            collection.Id, sequenceGroup.Id, order);

//        // Assert
//        Assert.True(result);

//        var association = await _dbContext.SequenceGroupCollectionSequenceGroups
//            .FirstOrDefaultAsync(sgc => sgc.SequenceGroupCollectionId == collection.Id
//                                     && sgc.SequenceGroupId == sequenceGroup.Id);
//        Assert.NotNull(association);
//        Assert.Equal(order, association.Order);
//    }

//    [Fact]
//    public async Task AddSequenceGroupToSequenceGroupCollectionAsync_WithInvalidCollectionId_ReturnsFalse()
//    {
//        // Arrange
//        var sequenceGroup = new SequenceGroup { Name = "Test Group" };
//        await _dbContext.SequenceGroups.AddAsync(sequenceGroup);
//        await _dbContext.SaveChangesAsync();

//        // Act
//        var result = await _service.AddSequenceGroupToSequenceGroupCollectionAsync(
//            -1, sequenceGroup.Id, 1);

//        // Assert
//        Assert.False(result);
//    }

//    [Fact]
//    public async Task AddSequenceGroupToSequenceGroupCollectionAsync_WithInvalidSequenceGroupId_ReturnsFalse()
//    {
//        // Arrange
//        var collection = new SequenceGroupCollection<TestEnum>
//        {
//            Name = "Test Collection",
//            Category = TestEnum.CategoryA
//        };
//        await _dbContext.SequenceGroupCollections.AddAsync(collection);
//        await _dbContext.SaveChangesAsync();

//        // Act
//        var result = await _service.AddSequenceGroupToSequenceGroupCollectionAsync(
//            collection.Id, -1, 1);

//        // Assert
//        Assert.False(result);
//    }

//    private enum TestEnum
//    {
//        CategoryA,
//        CategoryB
//    }
//}
