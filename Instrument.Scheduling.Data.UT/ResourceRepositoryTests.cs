using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Entities.Enums;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Repository;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Instrument.Scheduling.Data.UT;
public class ResourceRepositoryTests
{
    private readonly Mock<SchedulerDbContext> _mockDbContext;
    private readonly Mock<DbSet<Resource>> _mockResourceDbSet;
    private readonly Mock<DbSet<Parameter>> _mockParameterDbSet;
    private readonly ResourceRepository _repository;
    
    public ResourceRepositoryTests()
    {
        // Create mock DbSets
        _mockResourceDbSet = MockDbSetSetup();
        _mockParameterDbSet = MockDbSetSetup<Parameter>();
        
        // Create mock DbContext
        _mockDbContext = new Mock<SchedulerDbContext>(new DbContextOptionsBuilder<SchedulerDbContext>().Options);
        _mockDbContext.Setup(db => db.Resources).Returns(_mockResourceDbSet.Object);
        _mockDbContext.Setup(db => db.Parameters).Returns(_mockParameterDbSet.Object);
        
        // Create repository
        _repository = new ResourceRepository(_mockDbContext.Object);
    }
    
    private Mock<DbSet<T>> MockDbSetSetup<T>() where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        var data = new List<T>().AsQueryable();
        
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        
        return mockSet;
    }
    
    private Mock<DbSet<Resource>> MockDbSetSetup()
    {
        var resources = new List<Resource>
        {
            new Resource { Id = "res1", Name = "Resource 1", Code = "R1"},
            new Resource { Id = "res2", Name = "Resource 2", Code = "R2"}
        }.AsQueryable();
        
        var mockSet = new Mock<DbSet<Resource>>();
        mockSet.As<IQueryable<Resource>>().Setup(m => m.Provider).Returns(resources.Provider);
        mockSet.As<IQueryable<Resource>>().Setup(m => m.Expression).Returns(resources.Expression);
        mockSet.As<IQueryable<Resource>>().Setup(m => m.ElementType).Returns(resources.ElementType);
        mockSet.As<IQueryable<Resource>>().Setup(m => m.GetEnumerator()).Returns(resources.GetEnumerator());
        
        mockSet.Setup(m => m.Find(It.IsAny<object[]>())).Returns<object[]>(ids => 
        {
            var id = ids[0].ToString();
            return resources.FirstOrDefault(r => r.Id == id);
        });
        
        return mockSet;
    }
    
    [Fact]
    public async Task GetAllAsync_CallsDbContext_AndReturnsResult()
    {
        // Arrange
        _mockResourceDbSet.Setup(m => m.ToListAsync(default))
            .ReturnsAsync(new List<Resource>
            {
                new Resource { Id = "res1", Name = "Resource 1", Code = "R1" },
                new Resource { Id = "res2", Name = "Resource 2", Code = "R2" }
            });
        
        // Act
        var result = await _repository.GetAllAsync();
        
        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, r => r.Id == "res1");
        Assert.Contains(result, r => r.Id == "res2");
        _mockResourceDbSet.Verify(m => m.ToListAsync(default), Times.Once);
    }
    
    [Fact]
    public async Task GetByIdAsync_CallsDbContext_WithCorrectId()
    {
        // Arrange
        var resource = new Resource { 
            Id = "test-id", 
            Name = "Test Resource",
            Code = "TR",
        };
        
        _mockResourceDbSet.Setup(m => m.FindAsync(new object[] { "test-id" }))
            .ReturnsAsync(resource);
        
        // Act
        var result = await _repository.GetByIdAsync("test-id");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-id", result.Id);
        Assert.Equal("Test Resource", result.Name);
        Assert.Equal("TR", result.Code);
        _mockResourceDbSet.Verify(m => m.FindAsync(new object[] { "test-id" }), Times.Once);
    }
    
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenResourceNotFound()
    {
        // Arrange
        _mockResourceDbSet.Setup(m => m.FindAsync(new object[] { "non-existent" }))
            .ReturnsAsync((Resource)null);
        
        // Act
        var result = await _repository.GetByIdAsync("non-existent");
        
        // Assert
        Assert.Null(result);
        _mockResourceDbSet.Verify(m => m.FindAsync(new object[] { "non-existent" }), Times.Once);
    }
    
    [Fact]
    public async Task GetQueryableAsync_ReturnsQueryable()
    {
        // Act
        var result = await _repository.GetQueryableAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IQueryable<Resource>>(result);
    }
    
    [Fact]
    public async Task AddAsync_CallsDbContext_WithCorrectEntity()
    {
        // Arrange
        var resource = new Resource { 
            Id = "new-id", 
            Name = "New Resource",
            Code = "NR",
        };
        
        // Act
        await _repository.AddAsync(resource);
        
        // Assert
        _mockResourceDbSet.Verify(m => m.AddAsync(resource, default), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAsync_CallsDbContext_WithCorrectEntity()
    {
        // Arrange
        var resource = new Resource { 
            Id = "update-id", 
            Name = "Updated Resource",
            Code = "UR",
        };
        
        // Act
        await _repository.UpdateAsync(resource);
        
        // Assert
        _mockResourceDbSet.Verify(m => m.Update(resource), Times.Once);
    }
    
    [Fact]
    public async Task DeleteAsync_CallsDbContext_WithCorrectId()
    {
        // Arrange
        var resource = new Resource { 
            Id = "delete-id", 
            Name = "Delete Resource",
            Code = "DR",
        };
        
        _mockResourceDbSet.Setup(m => m.FindAsync(new object[] { "delete-id" }))
            .ReturnsAsync(resource);
        
        // Act
        await _repository.DeleteAsync("delete-id");
        
        // Assert
        _mockResourceDbSet.Verify(m => m.Remove(resource), Times.Once);
    }
    
    [Fact]
    public async Task DeleteAsync_ThrowsException_WhenEntityNotFound()
    {
        // Arrange
        _mockResourceDbSet.Setup(m => m.FindAsync(new object[] { "non-existent" }))
            .ReturnsAsync((Resource)null);
        
        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _repository.DeleteAsync("non-existent"));
    }
    
    [Fact]
    public async Task SaveChangesAsync_CallsDbContext_SaveChanges()
    {
        // Act
        await _repository.SaveChangesAsync();
        
        // Assert
        _mockDbContext.Verify(m => m.SaveChangesAsync(default), Times.Once);
    }
    
    [Fact]
    public async Task GetByCodeAsync_ReturnsCorrectResource()
    {
        // Arrange
        string code = "R1";
        
        var resources = new List<Resource>
        {
            new Resource { Id = "res1", Name = "Resource 1", Code = code },
            new Resource { Id = "res2", Name = "Resource 2", Code = "R2" }
        }.AsQueryable();
        
        var mockDbSet = new Mock<DbSet<Resource>>();
        mockDbSet.As<IQueryable<Resource>>().Setup(m => m.Provider).Returns(resources.Provider);
        mockDbSet.As<IQueryable<Resource>>().Setup(m => m.Expression).Returns(resources.Expression);
        mockDbSet.As<IQueryable<Resource>>().Setup(m => m.ElementType).Returns(resources.ElementType);
        mockDbSet.As<IQueryable<Resource>>().Setup(m => m.GetEnumerator()).Returns(resources.GetEnumerator());
        
        _mockDbContext.Setup(db => db.Resources).Returns(mockDbSet.Object);
        
        // Act
        var result = await _repository.GetByCodeAsync(code);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("res1", result.Id);
        Assert.Equal(code, result.Code);
    }
    
    [Fact]
    public async Task GetResourcesWithParametersAsync_ReturnsResourcesWithParameters()
    {
        // Arrange
        var resources = new List<Resource>
        {
            new Resource 
            { 
                Id = "res1", 
                Name = "Resource 1", 
                Code = "R1", 
                Parameters = new List<Parameter>
                {
                    new Parameter { Id = "param1", Name = "Parameter 1", Type = ParameterType.StringType }
                }
            }
        }.AsQueryable();
        
        var mockDbSet = new Mock<DbSet<Resource>>();
        mockDbSet.As<IQueryable<Resource>>().Setup(m => m.Provider).Returns(resources.Provider);
        mockDbSet.As<IQueryable<Resource>>().Setup(m => m.Expression).Returns(resources.Expression);
        mockDbSet.As<IQueryable<Resource>>().Setup(m => m.ElementType).Returns(resources.ElementType);
        mockDbSet.As<IQueryable<Resource>>().Setup(m => m.GetEnumerator()).Returns(resources.GetEnumerator());
        
        _mockDbContext.Setup(db => db.Resources).Returns(mockDbSet.Object);
        
        // Act
        var result = await _repository.GetResourcesWithParametersAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var resource = result.First();
        Assert.NotNull(resource.Parameters);
        Assert.Single(resource.Parameters);
    }
    
    [Fact]
    public async Task GetParametersForResourceAsync_ReturnsCorrectParameters()
    {
        // Arrange
        string resourceId = "res1";
        
        var parameters = new List<Parameter>
        {
            new Parameter { Id = "param1", Name = "Parameter 1", Type = ParameterType.StringType, ResourceId = resourceId },
            new Parameter { Id = "param2", Name = "Parameter 2", Type = ParameterType.IntegerType, ResourceId = resourceId },
            new Parameter { Id = "param3", Name = "Parameter 3", Type = ParameterType.BooleanType, ResourceId = "different-resource" }
        }.AsQueryable();
        
        var mockDbSet = new Mock<DbSet<Parameter>>();
        mockDbSet.As<IQueryable<Parameter>>().Setup(m => m.Provider).Returns(parameters.Provider);
        mockDbSet.As<IQueryable<Parameter>>().Setup(m => m.Expression).Returns(parameters.Expression);
        mockDbSet.As<IQueryable<Parameter>>().Setup(m => m.ElementType).Returns(parameters.ElementType);
        mockDbSet.As<IQueryable<Parameter>>().Setup(m => m.GetEnumerator()).Returns(parameters.GetEnumerator());
        
        _mockDbContext.Setup(db => db.Parameters).Returns(mockDbSet.Object);
        
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
        
        _mockResourceDbSet.Setup(m => m.FindAsync(new object[] { resourceId }))
            .ReturnsAsync(resource);
            
        _mockParameterDbSet.Setup(m => m.FindAsync(new object[] { parameterId }))
            .ReturnsAsync(parameter);
        
        // Act
        await _repository.AddParameterToResourceAsync(resourceId, parameterId);
        
        // Assert
        Assert.Equal(resourceId, parameter.ResourceId);
        _mockParameterDbSet.Verify(m => m.Update(parameter), Times.Once);
        _mockDbContext.Verify(m => m.SaveChangesAsync(default), Times.Once);
    }
    
    [Fact]
    public async Task RemoveParameterFromResourceAsync_UpdatesParameter()
    {
        // Arrange
        string resourceId = "res1";
        string parameterId = "param1";
        
        var parameter = new Parameter 
        { 
            Id = parameterId, 
            Name = "Parameter 1", 
            Type = ParameterType.StringType,
            ResourceId = resourceId
        };
        
        var parameters = new List<Parameter> { parameter }.AsQueryable();
        
        var mockDbSet = new Mock<DbSet<Parameter>>();
        mockDbSet.As<IQueryable<Parameter>>().Setup(m => m.Provider).Returns(parameters.Provider);
        mockDbSet.As<IQueryable<Parameter>>().Setup(m => m.Expression).Returns(parameters.Expression);
        mockDbSet.As<IQueryable<Parameter>>().Setup(m => m.ElementType).Returns(parameters.ElementType);
        mockDbSet.As<IQueryable<Parameter>>().Setup(m => m.GetEnumerator()).Returns(parameters.GetEnumerator());
        
        _mockDbContext.Setup(db => db.Parameters).Returns(mockDbSet.Object);
        
        // Act
        await _repository.RemoveParameterFromResourceAsync(resourceId, parameterId);
        
        // Assert
        Assert.Null(parameter.ResourceId);
        mockDbSet.Verify(m => m.Update(parameter), Times.Once);
        _mockDbContext.Verify(m => m.SaveChangesAsync(default), Times.Once);
    }
}