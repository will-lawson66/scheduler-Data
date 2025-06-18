namespace Instrument.Scheduling.Data.UT;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Execution.Parameter;
using Instrument.Scheduling.Data.Repository;
using Microsoft.EntityFrameworkCore;

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
    public async Task GetByCodeAsync_ReturnsCorrectResource()
    {
        // Arrange
        var code = "R1";
        
        await _dbContext.Resources.AddRangeAsync(
            new Resource { Name = "Resource 1", Code = code },
            new Resource { Name = "Resource 2", Code = "R2" }
        );
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetByCodeAsync(code);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(code, result.Code);
        Assert.Equal("Resource 1", result.Name);
    }
    
    [Fact]
    public async Task GetResourcesWithParametersAsync_ReturnsResourcesWithParameters()
    {
        // Arrange
        var resource = new Resource 
        { 
            Name = "Resource 1", 
            Code = "R1"
        };
        
        await _dbContext.Resources.AddAsync(resource);
        await _dbContext.SaveChangesAsync();
        
        var parameter = new Parameter 
        { 
            Name = "Parameter 1", 
            Type = ParameterType.StringType,
            ResourceId = resource.Id
        };
        
        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetResourcesWithParametersAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var fetchedResource = result.First();
        Assert.NotNull(fetchedResource.Parameters);
        Assert.Single(fetchedResource.Parameters);
        Assert.Equal(parameter.Id, fetchedResource.Parameters.First().Id);
    }
    
    [Fact]
    public async Task GetParametersForResourceAsync_ReturnsCorrectParameters()
    {
        // Arrange
        var resource = new Resource 
        { 
            Name = "Resource 1", 
            Code = "R1" 
        };
        
        await _dbContext.Resources.AddAsync(resource);
        await _dbContext.SaveChangesAsync();
        
        var parameters = new List<Parameter>
        {
            new()
                { Name = "Parameter 1", Type = ParameterType.StringType, ResourceId = resource.Id },
            new()
                { Name = "Parameter 2", Type = ParameterType.IntegerType, ResourceId = resource.Id },
            new()
                { Name = "Parameter 3", Type = ParameterType.BooleanType, ResourceId = resource.Id + 2 }
        };
        
        await _dbContext.Parameters.AddRangeAsync(parameters);
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetParametersForResourceAsync(resource.Id);
        
        // Assert
        var enumerable = result.ToList();
        Assert.Equal(2, enumerable.Count);
        Assert.Contains(enumerable, p => p.Name == "Parameter 1");
        Assert.Contains(enumerable, p => p.Name == "Parameter 2");
        Assert.DoesNotContain(enumerable, p => p.Name == "Parameter 3");
    }
    
    [Fact]
    public async Task AddParameterToResourceAsync_UpdatesParameter()
    {
        // Arrange
        var resource = new Resource { Name = "Resource 1", Code = "R1" };
        var parameter = new Parameter { Name = "Parameter 1", Type = ParameterType.StringType };
        
        await _dbContext.Resources.AddAsync(resource);
        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.SaveChangesAsync();
        
        // Act
        await _repository.AddParameterToResourceAsync(resource.Id, parameter.Id);
        
        // Assert
        var updatedParameter = await _dbContext.Parameters.FindAsync(parameter.Id);
        Assert.NotNull(updatedParameter);
        Assert.Equal(resource.Id, updatedParameter.ResourceId);
    }
    
    [Fact]
    public async Task RemoveParameterFromResourceAsync_UpdatesParameter()
    {
        // Arrange
        var resource = new Resource { Name = "Resource 1", Code = "R1" };
        await _dbContext.Resources.AddAsync(resource);

        var parameter = new Parameter 
        { 
            Name = "Parameter 1", 
            Type = ParameterType.StringType,
            ResourceId = resource.Id
        };
        
        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.SaveChangesAsync();
        
        // Act
        await _repository.RemoveParameterFromResourceAsync(resource.Id, parameter.Id);
        
        // Assert
        var updatedParameter = await _dbContext.Parameters.FindAsync(parameter.Id);
        Assert.NotNull(updatedParameter);
        Assert.Null(updatedParameter.ResourceId);
    }
}
