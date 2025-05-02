using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;
using Instrument.Data.Exceptions;
using Instrument.Data.Repository;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Data.UT;

public class ResourceRepositoryTests : IDisposable
{
    private readonly SchedulerDbContext _dbContext;
    private readonly ResourceRepository _repository;
    private readonly string _dbName;

    public ResourceRepositoryTests()
    {
        // Create a unique database name for each test run to ensure isolation
        _dbName = $"TestDB_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: _dbName)
            .Options;
        
        _dbContext = new SchedulerDbContext(options);
        _repository = new ResourceRepository(_dbContext);
    }

    public void Dispose()
    {
        // Clean up database after test
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
    
    [Fact]
    public async Task GetAllAsync_ReturnsAllResources()
    {
        // Arrange
        await _dbContext.Resources.AddRangeAsync(
            new Resource { Id = "res1", Name = "Resource 1", Code = "R1" },
            new Resource { Id = "res2", Name = "Resource 2", Code = "R2" }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        var resources = result.ToList();
        Assert.Equal(2, resources.Count);
        Assert.Contains(resources, r => r.Id == "res1");
        Assert.Contains(resources, r => r.Id == "res2");
    }
    
    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsResource()
    {
        // Arrange
        var resource = new Resource
        { 
            Id = "res-id",
            Name = "Test Resource", 
            Code = "TR"
        };
        
        await _dbContext.Resources.AddAsync(resource);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync("res-id");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("res-id", result.Id);
        Assert.Equal("Test Resource", result.Name);
        Assert.Equal("TR", result.Code);
    }
    
    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync("non-existent");
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetQueryableAsync_ReturnsQueryable()
    {
        // Arrange
        await _dbContext.Resources.AddRangeAsync(
            new Resource { Id = "res1", Name = "Resource 1", Code = "R1" },
            new Resource { Id = "res2", Name = "Resource 2", Code = "R2" }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetQueryableAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IQueryable<Resource>>(result);
        Assert.Equal(2, result.Count());
    }
    
    [Fact]
    public async Task AddAsync_AddsResource()
    {
        // Arrange
        var resource = new Resource
        { 
            Id = "new-id", 
            Name = "New Resource",
            Code = "NR"
        };
        
        // Act
        await _repository.AddAsync(resource);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _dbContext.Resources.FindAsync("new-id");
        Assert.NotNull(result);
        Assert.Equal("New Resource", result.Name);
        Assert.Equal("NR", result.Code);
    }
    
    [Fact]
    public async Task UpdateAsync_UpdatesResource()
    {
        // Arrange
        var original = new Resource
        { 
            Id = "res-id", 
            Name = "Original Resource",
            Code = "OR"
        };
        
        await _dbContext.Resources.AddAsync(original);
        await _dbContext.SaveChangesAsync();
        
        var updated = new Resource
        { 
            Id = "res-id", 
            Name = "Updated Resource",
            Code = "UR"
        };

        // Act
        await _repository.UpdateAsync(updated);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _dbContext.Resources.FindAsync("res-id");
        Assert.NotNull(result);
        Assert.Equal("Updated Resource", result.Name);
        Assert.Equal("UR", result.Code);
    }
    
    [Fact]
    public async Task DeleteAsync_DeletesResource()
    {
        // Arrange
        var resource = new Resource
        { 
            Id = "delete-id", 
            Name = "Delete Resource",
            Code = "DR"
        };
        
        await _dbContext.Resources.AddAsync(resource);
        await _dbContext.SaveChangesAsync();
        
        // Act
        await _repository.DeleteAsync("delete-id");
        await _repository.SaveChangesAsync();
        
        // Assert
        var result = await _dbContext.Resources.FindAsync("delete-id");
        Assert.Null(result);
    }
    
    [Fact]
    public async Task DeleteAsync_WithInvalidId_ThrowsEntityNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _repository.DeleteAsync("non-existent"));
    }
    
    [Fact]
    public async Task GetByCodeAsync_ReturnsCorrectResource()
    {
        // Arrange
        string code = "R1";
        
        await _dbContext.Resources.AddRangeAsync(
            new Resource { Id = "res1", Name = "Resource 1", Code = code },
            new Resource { Id = "res2", Name = "Resource 2", Code = "R2" }
        );
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetByCodeAsync(code);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("res1", result.Id);
        Assert.Equal(code, result.Code);
        Assert.Equal("Resource 1", result.Name);
    }
    
    [Fact]
    public async Task GetResourcesWithParametersAsync_ReturnsResourcesWithParameters()
    {
        // Arrange
        var resource = new Resource 
        { 
            Id = "res1", 
            Name = "Resource 1", 
            Code = "R1"
        };
        
        await _dbContext.Resources.AddAsync(resource);
        await _dbContext.SaveChangesAsync();
        
        var parameter = new Parameter 
        { 
            Id = "param1", 
            Name = "Parameter 1", 
            Type = ParameterType.StringType,
            ResourceId = "res1"
        };
        
        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetResourcesWithParametersAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var fetchedResource = result.First();
        Assert.Equal("res1", fetchedResource.Id);
        Assert.NotNull(fetchedResource.Parameters);
        Assert.Single(fetchedResource.Parameters);
        Assert.Equal("param1", fetchedResource.Parameters.First().Id);
    }
    
    [Fact]
    public async Task GetParametersForResourceAsync_ReturnsCorrectParameters()
    {
        // Arrange
        string resourceId = "res1";
        
        var resource = new Resource 
        { 
            Id = resourceId, 
            Name = "Resource 1", 
            Code = "R1" 
        };
        
        await _dbContext.Resources.AddAsync(resource);
        await _dbContext.SaveChangesAsync();
        
        var parameters = new List<Parameter>
        {
            new Parameter { Id = "param1", Name = "Parameter 1", Type = ParameterType.StringType, ResourceId = resourceId },
            new Parameter { Id = "param2", Name = "Parameter 2", Type = ParameterType.IntegerType, ResourceId = resourceId },
            new Parameter { Id = "param3", Name = "Parameter 3", Type = ParameterType.BooleanType, ResourceId = "different-resource" }
        };
        
        await _dbContext.Parameters.AddRangeAsync(parameters);
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetParametersForResourceAsync(resourceId);
        
        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, p => p.Id == "param1");
        Assert.Contains(result, p => p.Id == "param2");
        Assert.DoesNotContain(result, p => p.Id == "param3");
    }
    
    [Fact]
    public async Task AddParameterToResourceAsync_UpdatesParameter()
    {
        // Arrange
        string resourceId = "res1";
        string parameterId = "param1";
        
        var resource = new Resource { Id = resourceId, Name = "Resource 1", Code = "R1" };
        var parameter = new Parameter { Id = parameterId, Name = "Parameter 1", Type = ParameterType.StringType };
        
        await _dbContext.Resources.AddAsync(resource);
        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.SaveChangesAsync();
        
        // Act
        await _repository.AddParameterToResourceAsync(resourceId, parameterId);
        
        // Assert
        var updatedParameter = await _dbContext.Parameters.FindAsync(parameterId);
        Assert.NotNull(updatedParameter);
        Assert.Equal(resourceId, updatedParameter.ResourceId);
    }
    
    [Fact]
    public async Task RemoveParameterFromResourceAsync_UpdatesParameter()
    {
        // Arrange
        string resourceId = "res1";
        string parameterId = "param1";
        
        var resource = new Resource { Id = resourceId, Name = "Resource 1", Code = "R1" };
        var parameter = new Parameter 
        { 
            Id = parameterId, 
            Name = "Parameter 1", 
            Type = ParameterType.StringType,
            ResourceId = resourceId
        };
        
        await _dbContext.Resources.AddAsync(resource);
        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.SaveChangesAsync();
        
        // Act
        await _repository.RemoveParameterFromResourceAsync(resourceId, parameterId);
        
        // Assert
        var updatedParameter = await _dbContext.Parameters.FindAsync(parameterId);
        Assert.NotNull(updatedParameter);
        Assert.Null(updatedParameter.ResourceId);
    }
}
