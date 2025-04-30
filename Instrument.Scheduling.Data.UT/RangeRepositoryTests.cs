using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Entities.Enums;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Repository;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Instrument.Scheduling.Data.UT;
public class RangeRepositoryTests
{
    private readonly Mock<SchedulerDbContext> _mockDbContext;
    private readonly Mock<DbSet<Entities.Range>> _mockRangeDbSet;
    private readonly Mock<DbSet<RangeValue>> _mockRangeValueDbSet;
    private readonly Mock<DbSet<Parameter>> _mockParameterDbSet;
    private readonly RangeRepository _repository;
    
    public RangeRepositoryTests()
    {
        // Create mock DbSets
        _mockRangeDbSet = MockDbSetSetup();
        _mockRangeValueDbSet = MockDbSetSetup<RangeValue>();
        _mockParameterDbSet = MockDbSetSetup<Parameter>();
        
        // Create mock DbContext
        _mockDbContext = new Mock<SchedulerDbContext>(new DbContextOptionsBuilder<SchedulerDbContext>().Options);
        _mockDbContext.Setup(db => db.Ranges).Returns(_mockRangeDbSet.Object);
        _mockDbContext.Setup(db => db.RangeValues).Returns(_mockRangeValueDbSet.Object);
        _mockDbContext.Setup(db => db.Parameters).Returns(_mockParameterDbSet.Object);
        
        // Create repository
        _repository = new RangeRepository(_mockDbContext.Object);
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
    
    private Mock<DbSet<Entities.Range>> MockDbSetSetup()
    {
        var ranges = new List<Entities.Range>
        {
            new Entities.Range { Id = "range1", Name = "Range 1", Description = "Range 1 description" },
            new Entities.Range { Id = "range2", Name = "Range 2", Description = "Range 2 description" }
        }.AsQueryable();
        
        var mockSet = new Mock<DbSet<Entities.Range>>();
        mockSet.As<IQueryable<Entities.Range>>().Setup(m => m.Provider).Returns(ranges.Provider);
        mockSet.As<IQueryable<Entities.Range>>().Setup(m => m.Expression).Returns(ranges.Expression);
        mockSet.As<IQueryable<Entities.Range>>().Setup(m => m.ElementType).Returns(ranges.ElementType);
        mockSet.As<IQueryable<Entities.Range>>().Setup(m => m.GetEnumerator()).Returns(ranges.GetEnumerator());
        
        mockSet.Setup(m => m.Find(It.IsAny<object[]>())).Returns<object[]>(ids => 
        {
            var id = ids[0].ToString();
            return ranges.FirstOrDefault(r => r.Id == id);
        });
        
        return mockSet;
    }
    
    [Fact]
    public async Task GetAllAsync_CallsDbContext_AndReturnsResult()
    {
        // Arrange
        _mockRangeDbSet.Setup(m => m.ToListAsync(default))
            .ReturnsAsync(new List<Entities.Range>
            {
                new Entities.Range { Id = "range1", Name = "Range 1", Description = "Range 1 description" },
                new Entities.Range { Id = "range2", Name = "Range 2", Description = "Range 2 description" }
            });
        
        // Act
        var result = await _repository.GetAllAsync();
        
        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, r => r.Id == "range1");
        Assert.Contains(result, r => r.Id == "range2");
        _mockRangeDbSet.Verify(m => m.ToListAsync(default), Times.Once);
    }
    
    [Fact]
    public async Task GetByIdAsync_CallsDbContext_WithCorrectId()
    {
        // Arrange
        var range = new Entities.Range { 
            Id = "test-id", 
            Name = "Test Range",
            Description = "Test range description"
        };
        
        _mockRangeDbSet.Setup(m => m.FindAsync(new object[] { "test-id" }))
            .ReturnsAsync(range);
        
        // Act
        var result = await _repository.GetByIdAsync("test-id");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-id", result.Id);
        Assert.Equal("Test Range", result.Name);
        _mockRangeDbSet.Verify(m => m.FindAsync(new object[] { "test-id" }), Times.Once);
    }
    
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenRangeNotFound()
    {
        // Arrange
        _mockRangeDbSet.Setup(m => m.FindAsync(new object[] { "non-existent" }))
            .ReturnsAsync((Entities.Range)null);
        
        // Act
        var result = await _repository.GetByIdAsync("non-existent");
        
        // Assert
        Assert.Null(result);
        _mockRangeDbSet.Verify(m => m.FindAsync(new object[] { "non-existent" }), Times.Once);
    }
    
    [Fact]
    public async Task GetQueryableAsync_ReturnsQueryable()
    {
        // Act
        var result = await _repository.GetQueryableAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IQueryable<Entities.Range>>(result);
    }
    
    [Fact]
    public async Task AddAsync_CallsDbContext_WithCorrectEntity()
    {
        // Arrange
        var range = new Entities.Range { 
            Id = "new-id", 
            Name = "New Range",
            Description = "New range description"
        };
        
        // Act
        await _repository.AddAsync(range);
        
        // Assert
        _mockRangeDbSet.Verify(m => m.AddAsync(range, default), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAsync_CallsDbContext_WithCorrectEntity()
    {
        // Arrange
        var range = new Entities.Range { 
            Id = "update-id", 
            Name = "Updated Range",
            Description = "Updated range description"
        };
        
        // Act
        await _repository.UpdateAsync(range);
        
        // Assert
        _mockRangeDbSet.Verify(m => m.Update(range), Times.Once);
    }
    
    [Fact]
    public async Task DeleteAsync_CallsDbContext_WithCorrectId()
    {
        // Arrange
        var range = new Entities.Range { 
            Id = "delete-id", 
            Name = "Delete Range",
            Description = "Delete range description"
        };
        
        _mockRangeDbSet.Setup(m => m.FindAsync(new object[] { "delete-id" }))
            .ReturnsAsync(range);
        
        // Act
        await _repository.DeleteAsync("delete-id");
        
        // Assert
        _mockRangeDbSet.Verify(m => m.Remove(range), Times.Once);
    }
    
    [Fact]
    public async Task DeleteAsync_ThrowsException_WhenEntityNotFound()
    {
        // Arrange
        _mockRangeDbSet.Setup(m => m.FindAsync(new object[] { "non-existent" }))
            .ReturnsAsync((Entities.Range)null);
        
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
    public async Task GetRangeWithValuesAsync_ReturnsRangeWithValues()
    {
        // Arrange
        var range = new Entities.Range { 
            Id = "range1", 
            Name = "Range 1", 
            Description = "Range 1 description",
            Values = new List<RangeValue>
            {
                new RangeValue { Id = "val1", RangeId = "range1", Name = "Value 1", Value = "One" },
                new RangeValue { Id = "val2", RangeId = "range1", Name = "Value 2", Value = "Two" }
            }
        };
        
        var ranges = new List<Entities.Range> { range }.AsQueryable();
        
        var mockDbSet = new Mock<DbSet<Entities.Range>>();
        mockDbSet.As<IQueryable<Entities.Range>>().Setup(m => m.Provider).Returns(ranges.Provider);
        mockDbSet.As<IQueryable<Entities.Range>>().Setup(m => m.Expression).Returns(ranges.Expression);
        mockDbSet.As<IQueryable<Entities.Range>>().Setup(m => m.ElementType).Returns(ranges.ElementType);
        mockDbSet.As<IQueryable<Entities.Range>>().Setup(m => m.GetEnumerator()).Returns(ranges.GetEnumerator());
        
        _mockDbContext.Setup(db => db.Ranges).Returns(mockDbSet.Object);
        
        // Act
        var result = await _repository.GetRangeWithValuesAsync("range1");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("range1", result.Id);
        Assert.NotNull(result.Values);
        Assert.Equal(2, result.Values.Count);
    }
    
    [Fact]
    public async Task GetRangesByParameterAsync_ReturnsCorrectRanges()
    {
        // Arrange
        var parameterId = "param1";
        
        var parameters = new List<Parameter>
        {
            new Parameter 
            { 
                Id = parameterId, 
                Name = "Parameter 1", 
                Type = ParameterType.StringType,
                RangeId = "range1"
            }
        }.AsQueryable();
        
        var ranges = new List<Entities.Range>
        {
            new Entities.Range { Id = "range1", Name = "Range 1", Description = "Range 1 description" },
            new Entities.Range { Id = "range2", Name = "Range 2", Description = "Range 2 description" }
        }.AsQueryable();
        
        var mockParameterDbSet = new Mock<DbSet<Parameter>>();
        mockParameterDbSet.As<IQueryable<Parameter>>().Setup(m => m.Provider).Returns(parameters.Provider);
        mockParameterDbSet.As<IQueryable<Parameter>>().Setup(m => m.Expression).Returns(parameters.Expression);
        mockParameterDbSet.As<IQueryable<Parameter>>().Setup(m => m.ElementType).Returns(parameters.ElementType);
        mockParameterDbSet.As<IQueryable<Parameter>>().Setup(m => m.GetEnumerator()).Returns(parameters.GetEnumerator());
        
        var mockRangeDbSet = new Mock<DbSet<Entities.Range>>();
        mockRangeDbSet.As<IQueryable<Entities.Range>>().Setup(m => m.Provider).Returns(ranges.Provider);
        mockRangeDbSet.As<IQueryable<Entities.Range>>().Setup(m => m.Expression).Returns(ranges.Expression);
        mockRangeDbSet.As<IQueryable<Entities.Range>>().Setup(m => m.ElementType).Returns(ranges.ElementType);
        mockRangeDbSet.As<IQueryable<Entities.Range>>().Setup(m => m.GetEnumerator()).Returns(ranges.GetEnumerator());
        
        _mockDbContext.Setup(db => db.Parameters).Returns(mockParameterDbSet.Object);
        _mockDbContext.Setup(db => db.Ranges).Returns(mockRangeDbSet.Object);
        
        // Act
        var result = await _repository.GetRangesByParameterAsync(parameterId);
        
        // Assert
        Assert.Single(result);
        Assert.Equal("range1", result.First().Id);
    }
    
    [Fact]
    public async Task AddRangeValueAsync_AddsRangeValue()
    {
        // Arrange
        var rangeId = "range1";
        var name = "New Value";
        var value = "New";
        
        var range = new Entities.Range { Id = rangeId, Name = "Range 1", Description = "Range 1 description" };
        
        _mockRangeDbSet.Setup(m => m.FindAsync(new object[] { rangeId }))
            .ReturnsAsync(range);
            
       
        
        // Act
        var result = await _repository.AddRangeValueAsync(rangeId, name, value);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(rangeId, result.RangeId);
        Assert.Equal(name, result.Name);
        Assert.Equal(value, result.Value);
        
        _mockRangeValueDbSet.Verify(m => m.AddAsync(
            It.Is<RangeValue>(rv => 
                rv.RangeId == rangeId && 
                rv.Name == name && 
                rv.Value == value),
            default), 
            Times.Once);
        _mockDbContext.Verify(m => m.SaveChangesAsync(default), Times.Once);
    }
    
    [Fact]
    public async Task RemoveRangeValueAsync_RemovesRangeValue()
    {
        // Arrange
        var rangeValueId = "val1";
        var rangeValue = new RangeValue { Id = rangeValueId, RangeId = "range1", Name = "Value 1", Value = "One" };
        
        _mockRangeValueDbSet.Setup(m => m.FindAsync(new object[] { rangeValueId }))
            .ReturnsAsync(rangeValue);
        
        // Act
        await _repository.RemoveRangeValueAsync(rangeValueId);
        
        // Assert
        _mockRangeValueDbSet.Verify(m => m.Remove(rangeValue), Times.Once);
        _mockDbContext.Verify(m => m.SaveChangesAsync(default), Times.Once);
    }
    
    [Fact]
    public async Task GetParametersForRangeAsync_ReturnsCorrectParameters()
    {
        // Arrange
        var rangeId = "range1";
        
        var parameters = new List<Parameter>
        {
            new Parameter { Id = "param1", Name = "Parameter 1", Type = ParameterType.StringType, RangeId = rangeId },
            new Parameter { Id = "param2", Name = "Parameter 2", Type = ParameterType.IntegerType, RangeId = rangeId },
            new Parameter { Id = "param3", Name = "Parameter 3", Type = ParameterType.BooleanType, RangeId = "range2" }
        }.AsQueryable();
        
        var mockParameterDbSet = new Mock<DbSet<Parameter>>();
        mockParameterDbSet.As<IQueryable<Parameter>>().Setup(m => m.Provider).Returns(parameters.Provider);
        mockParameterDbSet.As<IQueryable<Parameter>>().Setup(m => m.Expression).Returns(parameters.Expression);
        mockParameterDbSet.As<IQueryable<Parameter>>().Setup(m => m.ElementType).Returns(parameters.ElementType);
        mockParameterDbSet.As<IQueryable<Parameter>>().Setup(m => m.GetEnumerator()).Returns(parameters.GetEnumerator());
        
        _mockDbContext.Setup(db => db.Parameters).Returns(mockParameterDbSet.Object);
        
        // Act
        var result = await _repository.GetParametersForRangeAsync(rangeId);
        
        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, p => p.Id == "param1");
        Assert.Contains(result, p => p.Id == "param2");
        Assert.DoesNotContain(result, p => p.Id == "param3");
    }
}
