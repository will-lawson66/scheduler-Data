using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Repository;
using Moq;

namespace Instrument.Scheduling.Data.UT;
public class ResourceRepositoryTests
{
    private readonly Mock<IStorageProvider<Resource>> _mockResourceProvider;
    private readonly Mock<IStorageProvider<Parameter>> _mockParameterProvider;
    private readonly ResourceRepository _repository;
    
    public ResourceRepositoryTests()
    {
        _mockResourceProvider = new Mock<IStorageProvider<Resource>>();
        _mockParameterProvider = new Mock<IStorageProvider<Parameter>>();
        _repository = new ResourceRepository(
            _mockResourceProvider.Object,
            _mockParameterProvider.Object);
    }
    
    [Fact]
    public async Task GetAllAsync_CallsProvider_AndReturnsResult()
    {
        // Arrange
        var resources = new List<Resource>
        {
            new Resource { Id = "res1", Name = "Resource 1", Code = "R1" },
            new Resource { Id = "res2", Name = "Resource 2", Code = "R2" }
        };
        
        _mockResourceProvider.Setup(p => p.GetAllAsync())
            .ReturnsAsync(resources);
        
        // Act
        var result = await _repository.GetAllAsync();
        
        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, r => r.Id == "res1");
        Assert.Contains(result, r => r.Id == "res2");
        _mockResourceProvider.Verify(p => p.GetAllAsync(), Times.Once);
    }
    
    [Fact]
    public async Task GetByIdAsync_CallsProvider_WithCorrectId()
    {
        // Arrange
        var resource = new Resource { 
            Id = "test-id", 
            Name = "Test Resource",
            Code = "TR"
        };
        
        _mockResourceProvider.Setup(p => p.GetByIdAsync("test-id"))
            .ReturnsAsync(resource);
        
        // Act
        var result = await _repository.GetByIdAsync("test-id");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-id", result.Id);
        Assert.Equal("Test Resource", result.Name);
        Assert.Equal("TR", result.Code);
        _mockResourceProvider.Verify(p => p.GetByIdAsync("test-id"), Times.Once);
    }
    
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenResourceNotFound()
    {
        // Arrange
        _mockResourceProvider.Setup(p => p.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Resource)null);
        
        // Act
        var result = await _repository.GetByIdAsync("non-existent");
        
        // Assert
        Assert.Null(result);
        _mockResourceProvider.Verify(p => p.GetByIdAsync("non-existent"), Times.Once);
    }
    
    [Fact]
    public async Task GetQueryableAsync_ReturnsQueryableData()
    {
        // Arrange
        var resources = new List<Resource>
        {
            new Resource { Id = "res1", Name = "Alpha Resource", Code = "AR" },
            new Resource { Id = "res2", Name = "Beta Resource", Code = "BR" },
            new Resource { Id = "res3", Name = "Gamma Resource", Code = "GR" }
        };
        
        _mockResourceProvider.Setup(p => p.GetAllAsync())
            .ReturnsAsync(resources);
        
        // Act
        var result = await _repository.GetQueryableAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        
        // Test queryability
        var filtered = result.Where(r => r.Name.StartsWith("B")).ToList();
        Assert.Single(filtered);
        Assert.Equal("Beta Resource", filtered[0].Name);
        
        _mockResourceProvider.Verify(p => p.GetAllAsync(), Times.Once);
    }
    
    [Fact]
    public async Task AddAsync_CallsProvider_WithCorrectEntity()
    {
        // Arrange
        var resource = new Resource { 
            Id = "new-id", 
            Name = "New Resource",
            Code = "NR"
        };
        
        // Act
        await _repository.AddAsync(resource);
        
        // Assert
        _mockResourceProvider.Verify(p => p.AddAsync(resource), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAsync_CallsProvider_WithCorrectEntity()
    {
        // Arrange
        var resource = new Resource { 
            Id = "update-id", 
            Name = "Updated Resource",
            Code = "UR"
        };
        
        // Act
        await _repository.UpdateAsync(resource);
        
        // Assert
        _mockResourceProvider.Verify(p => p.UpdateAsync(resource), Times.Once);
    }
    
    [Fact]
    public async Task DeleteAsync_CallsProvider_WithCorrectId()
    {
        // Arrange
        string idToDelete = "delete-id";
        
        // Act
        await _repository.DeleteAsync(idToDelete);
        
        // Assert
        _mockResourceProvider.Verify(p => p.DeleteAsync(idToDelete), Times.Once);
    }
    
    [Fact]
    public async Task SaveChangesAsync_CallsProvider_SaveChanges()
    {
        // Act
        await _repository.SaveChangesAsync();
        
        // Assert
        _mockResourceProvider.Verify(p => p.SaveChangesAsync(), Times.Once);
    }
    
    [Fact]
    public async Task GetParametersForResourceAsync_ReturnsCorrectParameters()
    {
        // Arrange
        string resourceId = "res1";
        
        var parameters = new List<Parameter>
        {
            new Parameter { 
                Id = "param1", 
                Name = "Parameter 1", 
                Type = "String",
                ResourceId = resourceId
            },
            new Parameter { 
                Id = "param2", 
                Name = "Parameter 2", 
                Type = "Integer",
                ResourceId = resourceId
            },
            new Parameter { 
                Id = "param3", 
                Name = "Parameter 3", 
                Type = "Boolean",
                ResourceId = "different-resource" // Not associated with the target resource
            }
        };
        
        _mockParameterProvider.Setup(p => p.GetAllAsync())
            .ReturnsAsync(parameters);
        
        // Act
        var result = await _repository.GetParametersForResourceAsync(resourceId);
        
        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, p => p.Id == "param1");
        Assert.Contains(result, p => p.Id == "param2");
        Assert.DoesNotContain(result, p => p.Id == "param3");
        
        _mockParameterProvider.Verify(p => p.GetAllAsync(), Times.Once);
    }
}