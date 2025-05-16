using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;
using Instrument.Data.Exceptions;
using Instrument.Data.Repository;
using Instrument.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Instrument.Data.UT;

public class SequenceServiceTests : IDisposable
{
    private readonly SchedulerDbContext _dbContext;
    private readonly ISequenceRepository _sequenceRepository;
    private readonly Mock<ILogger<SequenceService>> _mockLogger;
    private readonly SequenceService _service;

    public SequenceServiceTests()
    {
        // Set up in-memory database
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new SchedulerDbContext(options);

        // Set up real repository with in-memory database
        _sequenceRepository = new SequenceRepository(_dbContext);
        
        // Set up logger mock
        _mockLogger = new Mock<ILogger<SequenceService>>();

        // Create the service
        _service = new SequenceService(
            _sequenceRepository,
            _mockLogger.Object
        );
    }
    
    public void Dispose()
    {
        // Clean up database after test
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetSequenceAsync_WithValidId_ReturnsSequence()
    {
        // Arrange
        var sequence = new Sequence 
        { 
            Name = "Test Sequence", 
            WorstCaseTime = TimeSpan.FromSeconds(30) 
        };

        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetSequenceByIdAsync(sequence.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sequence.Id, result.Id);
        Assert.Equal("Test Sequence", result.Name);
        Assert.Equal(TimeSpan.FromSeconds(30), result.WorstCaseTime);
    }

    [Fact]
    public async Task CreateSequenceAsync_WithValidSequence_CreatesSequence()
    {
        // Arrange
        var sequence = new Sequence 
        { 
            Name = "Test Sequence", 
            WorstCaseTime = TimeSpan.FromSeconds(30) 
        };

        // Act
        await _service.CreateSequenceAsync(sequence);

        // Assert
        var createdSequence = await _dbContext.Sequences.FindAsync(sequence.Id);
        Assert.NotNull(createdSequence);
        Assert.Equal("Test Sequence", createdSequence.Name);
        Assert.Equal(TimeSpan.FromSeconds(30), createdSequence.WorstCaseTime);
    }


    [Fact]
    public async Task UpdateSequenceAsync_WithValidSequence_UpdatesSequence()
    {
        // Arrange
        var existingSequence = new Sequence 
        { 
            Name = "Original Sequence", 
            WorstCaseTime = TimeSpan.FromSeconds(45) 
        };
        
        await _dbContext.Sequences.AddAsync(existingSequence);
        await _dbContext.SaveChangesAsync();

        var updatedSequence = existingSequence.Update("Updated Sequence", TimeSpan.FromSeconds(30));
        

        // Act
        await _service.UpdateSequenceAsync(updatedSequence);

        // Assert
        var resultSequence = await _dbContext.Sequences.FindAsync(existingSequence.Id);
        Assert.NotNull(resultSequence);
        Assert.Equal("Updated Sequence", resultSequence.Name);
        Assert.Equal(TimeSpan.FromSeconds(30), resultSequence.WorstCaseTime);
    }

    [Fact]
    public async Task DeleteSequenceAsync_WithValidId_DeletesSequence()
    {
        // Arrange
        var existingSequence = new Sequence 
        { 
            Name = "Sequence to Delete", 
            WorstCaseTime = TimeSpan.FromSeconds(30) 
        };
        
        await _dbContext.Sequences.AddAsync(existingSequence);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.DeleteSequenceAsync(existingSequence.Id);

        // Assert
        var deletedSequence = await _dbContext.Sequences.FindAsync(existingSequence.Id);
        Assert.Null(deletedSequence);
    }

    [Fact]
    public async Task DeleteSequenceAsync_WithNonExistingId_ThrowsEntityNotFoundException()
    {
        // Arrange
        var id = -5;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.DeleteSequenceAsync(id));
            
        Assert.Equal(id, exception.EntityId);
        Assert.Equal("Sequence", exception.EntityType);
    }

    [Fact]
    public async Task GetAllSequencesAsync_ReturnsAllSequences()
    {
        // Arrange
        var sequences = new List<Sequence>
        {
            new()
                { Name = "Sequence 1", WorstCaseTime = TimeSpan.FromSeconds(30) },
            new()
                { Name = "Sequence 2", WorstCaseTime = TimeSpan.FromSeconds(45) }
        };

        await _dbContext.Sequences.AddRangeAsync(sequences);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetAllSequencesAsync();

        // Assert
        var enumerable = result.ToList();
        Assert.Equal(2, enumerable.Count);
        Assert.Contains(enumerable, s => s.Name == "Sequence 1");
        Assert.Contains(enumerable, s => s.Name == "Sequence 2");
    }

    [Fact]
    public async Task AddParameterToSequenceAsync_AddsParameterToSequence()
    {
        // Arrange
        var orderNumber = 1;

        var parameter = new Parameter { Name = "Test Parameter", Type = ParameterType.StringType };
        var sequence = new Sequence { Name = "Test Sequence", WorstCaseTime = TimeSpan.FromMilliseconds(30000) };

        await _dbContext.Parameters.AddAsync(parameter);
        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.AddParameterToSequenceAsync(parameter.Id, sequence.Id, orderNumber);

        // Assert
        var sequenceActual = await _dbContext.Sequences.FindAsync(sequence.Id);
        Assert.NotNull(sequenceActual);
        Assert.NotNull(sequenceActual.SequenceParameters);
        var sequenceParameter = sequenceActual.SequenceParameters.FirstOrDefault(sp => sp.ParameterId == parameter.Id
            && sp.SequenceId == sequence.Id);
    }

    [Fact]
    public async Task AddParameterToSequenceAsync_WithInvalidParameterId_ThrowsEntityNotFoundException()
    {
        // Arrange
        var parameterId = -2;
        var orderNumber = 1;

        var sequence = new Sequence { Name = "Test Sequence", WorstCaseTime = TimeSpan.FromMilliseconds(20000) };
        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _service.AddParameterToSequenceAsync(parameterId, sequence.Id, orderNumber));

        Assert.Equal(parameterId, exception.EntityId);
        Assert.Equal("Parameter", exception.EntityType);
    }

    [Fact]
    public async Task GetSequenceWithParameters_ReturnsSequenceWithParameters()
    {
        // Arrange
        // Create parameters
        var param1 = new Parameter { Name = "Parameter 1", Type = ParameterType.StringType };
        var param2 = new Parameter { Name = "Parameter 2", Type = ParameterType.IntegerType };

        // Create sequence
        var sequence = new Sequence { Name = "Test Sequence", WorstCaseTime = TimeSpan.FromSeconds(30) };

        // Add to database
        await _dbContext.Parameters.AddRangeAsync(param1, param2);
        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();

        // Create associations
        await _dbContext.SequenceParameters.AddRangeAsync(
            new SequenceParameter { SequenceId = sequence.Id, ParameterId = param1.Id, OrderNumber = 1 },
            new SequenceParameter { SequenceId = sequence.Id, ParameterId = param2.Id, OrderNumber = 2 }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetSequenceWithParametersAsync(sequence.Id);

        // Assert
        Assert.NotNull(result);
        var parameter1 = result.SequenceParameters.Select(p => p.ParameterId == param1.Id);
        //Assert.Equal(2, result.SequenceParameters);
       // Assert.Equal(2, result.SequenceParameters.;
    }

    [Fact]
    public async Task SearchSequencesAsync_WithPredicate_ReturnsFilteredSequences()
    {
        // Arrange
        var sequences = new List<Sequence>
        {
            new()
                { Name = "Alpha Sequence", WorstCaseTime = TimeSpan.FromSeconds(30) },
            new()
                { Name = "Beta Sequence", WorstCaseTime = TimeSpan.FromSeconds(45) }
        };

        await _dbContext.Sequences.AddRangeAsync(sequences);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.SearchSequencesAsync(s => s.Name.Contains("Alpha"));

        // Assert
        Assert.Single(result);
        Assert.Equal("Alpha Sequence", result.First().Name);
    }
}
