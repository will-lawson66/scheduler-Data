using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;
using Instrument.Data.Exceptions;
using Instrument.Data.Repository;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Data.UT;

/// <summary>
/// Tests for the generic <see cref="IRepository{T}"/> functionality. 
/// We test this once with Parameter as a representative entity type to avoid
/// repeating these tests for each repository.
/// </summary>
public class BaseRepositoryTests : IDisposable
{
private readonly SchedulerDbContext _dbContext;
private readonly ParameterRepository _repository;

    public BaseRepositoryTests()
    {
        var dbName = $"TestDB_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _dbContext = new SchedulerDbContext(options);
        _repository = new ParameterRepository(_dbContext);
    }
    public void Dispose()
    {
        // Clean up database after test
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAll()
    {
        // Arrange
        await _dbContext.Parameters.AddRangeAsync(
            new Parameter { Id = 1, Name = "Parameter 1", Type = ParameterType.StringType },
            new Parameter { Id = 2, Name = "Parameter 2", Type = ParameterType.IntegerType }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        var parameters = result.ToList();
        Assert.Equal(2, parameters.Count);
        Assert.Contains(parameters, p => p.Name == "Parameter 1");
        Assert.Contains(parameters, p => p.Name == "Parameter 2");
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsEntity()
    {
        // Arrange
        var parameter = new Parameter
        {
            Name = "Parameter 1",
            Type = ParameterType.StringType
        };

        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(parameter.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(parameter.Id, result.Id);
        Assert.Equal("Parameter 1", result.Name);
        Assert.Equal(ParameterType.StringType, result.Type);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(-2);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetQueryableAsync_ReturnsQueryable()
    {
        // Arrange
        await _dbContext.Parameters.AddRangeAsync(
            new Parameter { Name = "Parameter 1", Type = ParameterType.StringType },
            new Parameter { Name = "Parameter 2", Type = ParameterType.IntegerType }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetQueryableAsync();

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IQueryable<Parameter>>(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task AddAsync_AddsEntity()
    {
        // Arrange
        var parameter = new Parameter
        {
            Name = "New Parameter",
            Type = ParameterType.StringType
        };

        // Act
        await _repository.AddAsync(parameter);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _dbContext.Parameters.FindAsync(parameter.Id);
        Assert.NotNull(result);
        Assert.Equal("New Parameter", result.Name);
        Assert.Equal(ParameterType.StringType, result.Type);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntity()
    {
        // Arrange
        var original = new Parameter
        {
            Name = "Original Parameter",
            Type = ParameterType.StringType
        };

        await _dbContext.Parameters.AddAsync(original);
        await _dbContext.SaveChangesAsync();

        var updated = original.Update(name: "Updated Parameter", type: ParameterType.IntegerType);

        // Act
        await _repository.UpdateAsync(updated);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _dbContext.Parameters.FindAsync(updated.Id);
        Assert.NotNull(result);
        Assert.Equal("Updated Parameter", result.Name);
        Assert.Equal(ParameterType.IntegerType, result.Type);
    }

    [Fact]
    public async Task DeleteAsync_DeletesEntity()
    {
        // Arrange
        var parameter = new Parameter
        {
            Name = "Delete Parameter",
            Type = ParameterType.StringType
        };

        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(parameter.Id);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _dbContext.Parameters.FindAsync(parameter.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ThrowsEntityNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _repository.DeleteAsync(-1));
    }
}