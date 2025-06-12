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

    public class ResourceServiceTests : IDisposable
    {
        private readonly SchedulerDbContext _dbContext;
        private readonly IResourceRepository _resourceRepository;
        private readonly Mock<ILogger<ResourceService>> _mockLogger;
        private readonly ResourceService _service;
        private readonly string _dbName;

        public ResourceServiceTests()
        {
            _dbName = $"TestDb_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<SchedulerDbContext>()
                .UseInMemoryDatabase(databaseName: _dbName)
                .Options;
            
            _dbContext = new SchedulerDbContext(options);
            _resourceRepository = new ResourceRepository(_dbContext);
            _mockLogger = new Mock<ILogger<ResourceService>>();
            _service = new ResourceService(_resourceRepository, _mockLogger.Object);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Fact]
        public async Task GetResourceByIdAsync_WithValidId_ReturnsResource()
        {
            // Arrange
            var resource = new Resource 
            { 
                Name = "Test Resource", 
                Code = "TR001"
            };

            await _dbContext.Resources.AddAsync(resource);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetResourceByIdAsync(resource.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(resource.Id, result.Id);
            Assert.Equal("Test Resource", result.Name);
            Assert.Equal("TR001", result.Code);
        }

        [Fact]
        public async Task GetResourceByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _service.GetResourceByIdAsync(-1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateResourceAsync_WithValidResource_CreatesResource()
        {
            // Arrange
            var resource = new Resource 
            { 
                Name = "New Resource", 
                Code = "NR001"
            };

            // Act
            var result = await _service.CreateResourceAsync(resource);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Resource", result.Name);
            Assert.Equal("NR001", result.Code);
            Assert.True(result.Id > 0);

            var createdResource = await _dbContext.Resources.FindAsync(result.Id);
            Assert.NotNull(createdResource);
            Assert.Equal("New Resource", createdResource.Name);
        }

        [Fact]
        public async Task CreateResourceAsync_WithNullResource_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.CreateResourceAsync(null!));
        }

        [Fact]
        public async Task UpdateResourceAsync_WithValidResource_UpdatesResource()
        {
            // Arrange
            var existingResource = new Resource 
            { 
                Name = "Original Resource", 
                Code = "OR001"
            };
            
            await _dbContext.Resources.AddAsync(existingResource);
            await _dbContext.SaveChangesAsync();

            var updatedResource = existingResource.Update("Updated Resource", "UR001");

            // Act
            await _service.UpdateResourceAsync(updatedResource);

            // Assert
            var resultResource = await _dbContext.Resources.FindAsync(existingResource.Id);
            Assert.NotNull(resultResource);
            Assert.Equal("Updated Resource", resultResource.Name);
            Assert.Equal("UR001", resultResource.Code);
        }

        [Fact]
        public async Task UpdateResourceAsync_WithNullResource_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.UpdateResourceAsync(null!));
        }

        [Fact]
        public async Task DeleteResourceAsync_WithValidId_DeletesResource()
        {
            // Arrange
            var resource = new Resource 
            { 
                Name = "Resource to Delete", 
                Code = "RD001"
            };
            
            await _dbContext.Resources.AddAsync(resource);
            await _dbContext.SaveChangesAsync();

            // Act
            await _service.DeleteResourceAsync(resource.Id);

            // Assert
            var deletedResource = await _dbContext.Resources.FindAsync(resource.Id);
            Assert.Null(deletedResource);
        }

        [Fact]
        public async Task DeleteResourceAsync_WithInvalidId_ThrowsEntityNotFoundException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
                _service.DeleteResourceAsync(-1));

            Assert.Equal(-1, exception.EntityId);
            Assert.Equal("Resource", exception.EntityType);
        }

        [Fact]
        public async Task GetAllResourcesAsync_ReturnsAllResources()
        {
            // Arrange
            var resources = new List<Resource>
            {
                new() { Name = "Resource 1", Code = "R001" },
                new() { Name = "Resource 2", Code = "R002" }
            };

            await _dbContext.Resources.AddRangeAsync(resources);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetAllResourcesAsync();

            // Assert
            var resourceList = result.ToList();
            Assert.Equal(2, resourceList.Count);
            Assert.Contains(resourceList, r => r.Name == "Resource 1");
            Assert.Contains(resourceList, r => r.Name == "Resource 2");
        }

        [Fact]
        public void Constructor_WithNullRepository_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ResourceService(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ResourceService(_resourceRepository, null!));
        }

        // Test the NotImplemented methods to ensure they throw the expected exception
        [Fact]
        public void GetByCodeAsync_ThrowsNotImplementedException()
        {
            // Act & Assert
            Assert.ThrowsAsync<NotImplementedException>(() => 
                _service.GetByCodeAsync("TEST"));
        }

        [Fact]
        public void GetResourcesWithParametersAsync_ThrowsNotImplementedException()
        {
            // Act & Assert
            Assert.ThrowsAsync<NotImplementedException>(() => 
                _service.GetResourcesWithParametersAsync());
        }

        [Fact]
        public void AddParameterToResourceAsync_ThrowsNotImplementedException()
        {
            // Act & Assert
            Assert.ThrowsAsync<NotImplementedException>(() => 
                _service.AddParameterToResourceAsync(1, 1));
        }

        [Fact]
        public void RemoveParameterFromResourceAsync_ThrowsNotImplementedException()
        {
            // Act & Assert
            Assert.ThrowsAsync<NotImplementedException>(() => 
                _service.RemoveParameterFromResourceAsync(1, 1));
        }

        [Fact]
        public void GetParametersForResourceAsync_ThrowsNotImplementedException()
        {
            // Act & Assert
            Assert.ThrowsAsync<NotImplementedException>(() => 
                _service.GetParametersForResourceAsync(1));
        }
    }
}