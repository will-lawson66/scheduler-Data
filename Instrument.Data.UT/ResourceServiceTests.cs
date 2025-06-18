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
