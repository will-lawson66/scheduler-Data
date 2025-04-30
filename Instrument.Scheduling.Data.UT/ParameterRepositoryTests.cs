using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Entities.Enums;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Repository;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Instrument.Scheduling.Data.UT;
public class ParameterRepositoryTests
{
    private readonly Mock<SchedulerDbContext> _mockDbContext;
    private readonly Mock<DbSet<Parameter>> _mockParameterDbSet;
    private readonly Mock<DbSet<SequenceParameter>> _mockSequenceParameterDbSet;
    private readonly ParameterRepository _repository;
    
    public ParameterRepositoryTests()
    {
        // Create mock DbSets
        _mockParameterDbSet = MockDbSetSetup();
        _mockSequenceParameterDbSet = MockDbSetSetup<SequenceParameter>();
        
        // Create mock DbContext
        _mockDbContext = new Mock<SchedulerDbContext>(new DbContextOptionsBuilder<SchedulerDbContext>().Options);
        _mockDbContext.Setup(db => db.Parameters).Returns(_mockParameterDbSet.Object);
        _mockDbContext.Setup(db => db.SequenceParameters).Returns(_mockSequenceParameterDbSet.Object);
        
        // Create repository
        _repository = new ParameterRepository(_mockDbContext.Object);
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
    
    private Mock<DbSet<Parameter>> MockDbSetSetup()
    {
        var parameters = new List<Parameter>
        {
            new Parameter { Id = "param1", Name = "Parameter 1", Type = ParameterType.StringType },
            new Parameter { Id = "param2", Name = "Parameter 2", Type = ParameterType.IntegerType }
        }.AsQueryable();
        
        var mockSet = new Mock<DbSet<Parameter>>();
        mockSet.As<IQueryable<Parameter>>().Setup(m => m.Provider).Returns(parameters.Provider);
        mockSet.As<IQueryable<Parameter>>().Setup(m => m.Expression).Returns(parameters.Expression);
        mockSet.As<IQueryable<Parameter>>().Setup(m => m.ElementType).Returns(parameters.ElementType);
        mockSet.As<IQueryable<Parameter>>().Setup(m => m.GetEnumerator()).Returns(parameters.GetEnumerator());
        
        mockSet.Setup(m => m.Find(It.IsAny<object[]>())).Returns<object[]>(ids => 
        {
            var id = ids[0].ToString();
            return parameters.FirstOrDefault(p => p.Id == id);
        });
        
        return mockSet;
    }
    
    [Fact]
    public async Task GetAllAsync_CallsDbContext_AndReturnsResult()
    {
        // Arrange
        _mockParameterDbSet.Setup(m => m.ToListAsync(default))
            .ReturnsAsync(new List<Parameter>
            {
                new Parameter { Id = "param1", Name = "Parameter 1", Type = ParameterType.StringType },
                new Parameter { Id = "param2", Name = "Parameter 2", Type = ParameterType.IntegerType }
            });
        
        // Act
        var result = await _repository.GetAllAsync();
        
        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, p => p.Id == "param1");
        Assert.Contains(result, p => p.Id == "param2");
        _mockParameterDbSet.Verify(m => m.ToListAsync(default), Times.Once);
    }
    
    [Fact]
    public async Task GetByIdAsync_CallsDbContext_WithCorrectId()
    {
        // Arrange
        var parameter = new Parameter { 
            Id = "test-id", 
            Name = "Test Parameter",
            Type = ParameterType.StringType,
            DefaultValue = "Default"
        };
        
        _mockParameterDbSet.Setup(m => m.FindAsync(new object[] { "test-id" }))
            .ReturnsAsync(parameter);
        
        // Act
        var result = await _repository.GetByIdAsync("test-id");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-id", result.Id);
        Assert.Equal("Test Parameter", result.Name);
        Assert.Equal(ParameterType.StringType, result.Type);
        Assert.Equal("Default", result.DefaultValue);
        _mockParameterDbSet.Verify(m => m.FindAsync(new object[] { "test-id" }), Times.Once);
    }
    
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenParameterNotFound()
    {
        // Arrange
        _mockParameterDbSet.Setup(m => m.FindAsync(new object[] { "non-existent" }))
            .ReturnsAsync((Parameter)null);
        
        // Act
        var result = await _repository.GetByIdAsync("non-existent");
        
        // Assert
        Assert.Null(result);
        _mockParameterDbSet.Verify(m => m.FindAsync(new object[] { "non-existent" }), Times.Once);
    }
    
    [Fact]
    public async Task GetQueryableAsync_ReturnsQueryable()
    {
        // Act
        var result = await _repository.GetQueryableAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IQueryable<Parameter>>(result);
    }
    
    [Fact]
    public async Task AddAsync_CallsDbContext_WithCorrectEntity()
    {
        // Arrange
        var parameter = new Parameter { 
            Id = "new-id", 
            Name = "New Parameter",
            Type = ParameterType.StringType
        };
        
        // Act
        await _repository.AddAsync(parameter);
        
        // Assert
        _mockParameterDbSet.Verify(m => m.AddAsync(parameter, default), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAsync_CallsDbContext_WithCorrectEntity()
    {
        // Arrange
        var parameter = new Parameter { 
            Id = "update-id", 
            Name = "Updated Parameter",
            Type = ParameterType.StringType
        };
        
        // Act
        await _repository.UpdateAsync(parameter);
        
        // Assert
        _mockParameterDbSet.Verify(m => m.Update(parameter), Times.Once);
    }
    
    [Fact]
    public async Task DeleteAsync_CallsDbContext_WithCorrectId()
    {
        // Arrange
        var parameter = new Parameter { 
            Id = "delete-id", 
            Name = "Delete Parameter",
            Type = ParameterType.StringType
        };
        
        _mockParameterDbSet.Setup(m => m.FindAsync(new object[] { "delete-id" }))
            .ReturnsAsync(parameter);
        
        // Act
        await _repository.DeleteAsync("delete-id");
        
        // Assert
        _mockParameterDbSet.Verify(m => m.Remove(parameter), Times.Once);
    }
    
    [Fact]
    public async Task DeleteAsync_ThrowsException_WhenEntityNotFound()
    {
        // Arrange
        _mockParameterDbSet.Setup(m => m.FindAsync(new object[] { "non-existent" }))
            .ReturnsAsync((Parameter)null);
        
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
    public async Task GetParametersForSequenceAsync_ReturnsCorrectParameters()
    {
        // Arrange
        string sequenceId = "seq1";
        
        var sequenceParameters = new List<SequenceParameter>
        {
            new SequenceParameter { 
                SequenceId = sequenceId, 
                ParameterId = "param1",
                Parameter = new Parameter { Id = "param1", Name = "Parameter 1", Type = ParameterType.StringType }
            },
            new SequenceParameter { 
                SequenceId = sequenceId, 
                ParameterId = "param2",
                Parameter = new Parameter { Id = "param2", Name = "Parameter 2", Type = ParameterType.IntegerType }
            }
        }.AsQueryable();
        
        var mockDbSet = new Mock<DbSet<SequenceParameter>>();
        mockDbSet.As<IQueryable<SequenceParameter>>().Setup(m => m.Provider).Returns(sequenceParameters.Provider);
        mockDbSet.As<IQueryable<SequenceParameter>>().Setup(m => m.Expression).Returns(sequenceParameters.Expression);
        mockDbSet.As<IQueryable<SequenceParameter>>().Setup(m => m.ElementType).Returns(sequenceParameters.ElementType);
        mockDbSet.As<IQueryable<SequenceParameter>>().Setup(m => m.GetEnumerator()).Returns(sequenceParameters.GetEnumerator());
        
        _mockDbContext.Setup(db => db.SequenceParameters).Returns(mockDbSet.Object);
        
        // Act
        var result = await _repository.GetParametersForSequenceAsync(sequenceId);
        
        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, p => p.Id == "param1");
        Assert.Contains(result, p => p.Id == "param2");
    }
    
    [Fact]
    public async Task GetParametersByTypeAsync_ReturnsCorrectParameters()
    {
        // Arrange
        var parameters = new List<Parameter>
        {
            new Parameter { Id = "param1", Name = "Parameter 1", Type = ParameterType.StringType },
            new Parameter { Id = "param2", Name = "Parameter 2", Type = ParameterType.IntegerType },
            new Parameter { Id = "param3", Name = "Parameter 3", Type = ParameterType.StringType }
        }.AsQueryable();
        
        var mockDbSet = new Mock<DbSet<Parameter>>();
        mockDbSet.As<IQueryable<Parameter>>().Setup(m => m.Provider).Returns(parameters.Provider);
        mockDbSet.As<IQueryable<Parameter>>().Setup(m => m.Expression).Returns(parameters.Expression);
        mockDbSet.As<IQueryable<Parameter>>().Setup(m => m.ElementType).Returns(parameters.ElementType);
        mockDbSet.As<IQueryable<Parameter>>().Setup(m => m.GetEnumerator()).Returns(parameters.GetEnumerator());
        
        _mockDbContext.Setup(db => db.Parameters).Returns(mockDbSet.Object);
        
        // Act
        var result = await _repository.GetParametersByTypeAsync(ParameterType.StringType);
        
        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Equal(ParameterType.StringType, p.Type));
    }
    
    [Fact]
    public async Task AddParameterToSequenceAsync_AddsSequenceParameter()
    {
        // Arrange
        string parameterId = "param1";
        string sequenceId = "seq1";
        int orderNumber = 1;
        
        var parameter = new Parameter { Id = parameterId, Name = "Parameter 1", Type = ParameterType.StringType };
        var sequence = new Sequence { Id = sequenceId, Name = "Sequence 1", WorstCaseTime = TimeSpan.Zero };
        
        _mockParameterDbSet.Setup(m => m.FindAsync(new object[] { parameterId }))
            .ReturnsAsync(parameter);
            
        _mockDbContext.Setup(m => m.Sequences.FindAsync(new object[] { sequenceId }))
            .ReturnsAsync(sequence);
            
        // Act
        await _repository.AddParameterToSequenceAsync(parameterId, sequenceId, orderNumber);
        
        // Assert
        _mockSequenceParameterDbSet.Verify(m => m.AddAsync(
            It.Is<SequenceParameter>(sp => 
                sp.ParameterId == parameterId && 
                sp.SequenceId == sequenceId &&
                sp.OrderNumber == orderNumber),
            default), 
            Times.Once);
        _mockDbContext.Verify(m => m.SaveChangesAsync(default), Times.Once);
    }
    
    [Fact]
    public async Task RemoveParameterFromSequenceAsync_RemovesSequenceParameter()
    {
        // Arrange
        string parameterId = "param1";
        string sequenceId = "seq1";
        
        var sequenceParameter = new SequenceParameter 
        { 
            ParameterId = parameterId, 
            SequenceId = sequenceId, 
            OrderNumber = 1 
        };
        
        var sequenceParameters = new List<SequenceParameter> { sequenceParameter }.AsQueryable();
        
        var mockDbSet = new Mock<DbSet<SequenceParameter>>();
        mockDbSet.As<IQueryable<SequenceParameter>>().Setup(m => m.Provider).Returns(sequenceParameters.Provider);
        mockDbSet.As<IQueryable<SequenceParameter>>().Setup(m => m.Expression).Returns(sequenceParameters.Expression);
        mockDbSet.As<IQueryable<SequenceParameter>>().Setup(m => m.ElementType).Returns(sequenceParameters.ElementType);
        mockDbSet.As<IQueryable<SequenceParameter>>().Setup(m => m.GetEnumerator()).Returns(sequenceParameters.GetEnumerator());
        
        _mockDbContext.Setup(db => db.SequenceParameters).Returns(mockDbSet.Object);
        
        // Act
        await _repository.RemoveParameterFromSequenceAsync(parameterId, sequenceId);
        
        // Assert
        mockDbSet.Verify(m => m.Remove(sequenceParameter), Times.Once);
        _mockDbContext.Verify(m => m.SaveChangesAsync(default), Times.Once);
    }
}
