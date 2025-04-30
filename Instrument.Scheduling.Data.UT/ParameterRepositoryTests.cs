using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Repository;
using Instrument.Scheduling.Data.Entities.Enums;
using Moq;

namespace Instrument.Scheduling.Data.UT;
public class ParameterRepositoryTests
{
    private readonly Mock<IStorageProvider<Parameter>> _mockParameterProvider;
    private readonly Mock<IStorageProvider<SequenceParameter>> _mockSequenceParameterProvider;
    private readonly ParameterRepository _repository;
    
    public ParameterRepositoryTests()
    {
        _mockParameterProvider = new Mock<IStorageProvider<Parameter>>();
        _mockSequenceParameterProvider = new Mock<IStorageProvider<SequenceParameter>>();
        _repository = new ParameterRepository(
            _mockParameterProvider.Object,
            _mockSequenceParameterProvider.Object);
    }
    
    [Fact]
    public async Task GetAllAsync_CallsProvider_AndReturnsResult()
    {
        // Arrange
        var parameters = new List<Parameter>
        {
            new Parameter { Id = "param1", Name = "Parameter 1", Type = ParameterType.StringType },
            new Parameter { Id = "param2", Name = "Parameter 2", Type = ParameterType.IntegerType }
        };
        
        _mockParameterProvider.Setup(p => p.GetAllAsync())
            .ReturnsAsync(parameters);
        
        // Act
        var result = await _repository.GetAllAsync();
        
        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, p => p.Id == "param1");
        Assert.Contains(result, p => p.Id == "param2");
        _mockParameterProvider.Verify(p => p.GetAllAsync(), Times.Once);
    }
    
    [Fact]
    public async Task GetByIdAsync_CallsProvider_WithCorrectId()
    {
        // Arrange
        var parameter = new Parameter { 
            Id = "test-id", 
            Name = "Test Parameter",
            Type = ParameterType.StringType,
            DefaultValue = "Default"
        };
        
        _mockParameterProvider.Setup(p => p.GetByIdAsync("test-id"))
            .ReturnsAsync(parameter);
        
        // Act
        var result = await _repository.GetByIdAsync("test-id");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-id", result.Id);
        Assert.Equal("Test Parameter", result.Name);
        Assert.Equal(ParameterType.StringType, result.Type);
        Assert.Equal("Default", result.DefaultValue);
        _mockParameterProvider.Verify(p => p.GetByIdAsync("test-id"), Times.Once);
    }
    
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenParameterNotFound()
    {
        // Arrange
        _mockParameterProvider.Setup(p => p.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Parameter)null);
        
        // Act
        var result = await _repository.GetByIdAsync("non-existent");
        
        // Assert
        Assert.Null(result);
        _mockParameterProvider.Verify(p => p.GetByIdAsync("non-existent"), Times.Once);
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
            },
            new SequenceParameter { 
                SequenceId = sequenceId, 
                ParameterId = "param2",
            }
        };
        
        var parameters = new List<Parameter>
        {
            new Parameter { Id = "param1", Name = "Parameter 1", Type = ParameterType.StringType },
            new Parameter { Id = "param2", Name = "Parameter 2", Type = ParameterType.IntegerType },
            new Parameter { Id = "param3", Name = "Parameter 3", Type = ParameterType.BooleanType} 
        };
        
        _mockSequenceParameterProvider.Setup(p => p.GetAllAsync())
            .ReturnsAsync(sequenceParameters);
            
        _mockParameterProvider.Setup(p => p.GetAllAsync())
            .ReturnsAsync(parameters);
        
        // Act
        var result = await _repository.GetParametersForSequenceAsync(sequenceId);
        
        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, p => p.Id == "param1");
        Assert.Contains(result, p => p.Id == "param2");
        Assert.DoesNotContain(result, p => p.Id == "param3");
        
        _mockSequenceParameterProvider.Verify(p => p.GetAllAsync(), Times.Once);
        _mockParameterProvider.Verify(p => p.GetAllAsync(), Times.Once);
    }
    
    [Fact]
    public async Task GetQueryableAsync_ReturnsQueryableData()
    {
        // Arrange
        var parameters = new List<Parameter>
        {
            new Parameter { Id = "param1", Name = "Alpha Parameter", Type = ParameterType.StringType },
            new Parameter { Id = "param2", Name = "Beta Parameter", Type = ParameterType.IntegerType },
            new Parameter { Id = "param3", Name = "Gamma Parameter", Type = ParameterType.BooleanType }
        };
        
        _mockParameterProvider.Setup(p => p.GetAllAsync())
            .ReturnsAsync(parameters);
        
        // Act
        var result = await _repository.GetQueryableAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        
        // Test queryability
        var filtered = result.Where(p => p.Name.StartsWith("B")).ToList();
        Assert.Single(filtered);
        Assert.Equal("Beta Parameter", filtered[0].Name);
        
        _mockParameterProvider.Verify(p => p.GetAllAsync(), Times.Once);
    }
    
    [Fact]
    public async Task AddAsync_CallsProvider_WithCorrectEntity()
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
        _mockParameterProvider.Verify(p => p.AddAsync(parameter), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAsync_CallsProvider_WithCorrectEntity()
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
        _mockParameterProvider.Verify(p => p.UpdateAsync(parameter), Times.Once);
    }
    
    [Fact]
    public async Task DeleteAsync_CallsProvider_WithCorrectId()
    {
        // Arrange
        string idToDelete = "delete-id";
        
        // Act
        await _repository.DeleteAsync(idToDelete);
        
        // Assert
        _mockParameterProvider.Verify(p => p.DeleteAsync(idToDelete), Times.Once);
    }
    
    [Fact]
    public async Task SaveChangesAsync_CallsProviders_SaveChanges()
    {
        // Act
        await _repository.SaveChangesAsync();
        
        // Assert
        _mockParameterProvider.Verify(p => p.SaveChangesAsync(), Times.Once);
        _mockSequenceParameterProvider.Verify(p => p.SaveChangesAsync(), Times.Once);
    }
    
    [Fact]
    public async Task AddParameterToSequence_AddsSequenceParameter()
    {
        // Arrange
        string sequenceId = "seq1";
        string parameterId = "param1";
        var parameterOrder = 1;
        
        // Act
        await _repository.AddParameterToSequenceAsync(sequenceId, parameterId, parameterOrder);
        
        // Assert
        _mockSequenceParameterProvider.Verify(p => p.AddAsync(
            It.Is<SequenceParameter>(sp => 
                sp.SequenceId == sequenceId && 
                sp.ParameterId == parameterId &&
                sp.OrderNumber == parameterOrder)), 
            Times.Once);
    }
    
    [Fact]
    public async Task RemoveParameterFromSequence_DeletesSequenceParameter()
    {
        // Arrange
        string sequenceId = "seq1";
        string parameterId = "param1";
        
        // Act
        await _repository.RemoveParameterFromSequenceAsync(sequenceId, parameterId);
        
        // Assert
        _mockSequenceParameterProvider.Verify(p => p.DeleteAsync(
            It.Is<string>(id => id.Contains(sequenceId) && id.Contains(parameterId))), 
            Times.Once);
    }
}
