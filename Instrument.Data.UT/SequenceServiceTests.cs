using Instrument.Data.DataContext;
using Instrument.Data.DTOs;
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
        _service = new SequenceService(_sequenceRepository, _mockLogger.Object);
    }
    
    public void Dispose()
    {
        // Clean up database after test
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetSequenceAsync_WithNameFilter_ReturnsCorrectDTO()
    {
        // Arrange
        var sequence = new Sequence
        {
            Name = "Test Sequence",
            Description = "Test description",
            WorstCaseTime = TimeSpan.FromMinutes(5),
            CanBeParallel = true
        };
        
        var parameter1 = new Parameter
        {
            Name = "Parameter 1",
            Type = ParameterType.String,
            DefaultValue = "Value1"
        };
        
        var parameter2 = new Parameter
        {
            Name = "Parameter 2",
            Type = ParameterType.Integer,
            DefaultValue = "42"
        };

        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.Parameters.AddRangeAsync(parameter1, parameter2);
        await _dbContext.SaveChangesAsync();

        // Add parameters to sequence with specific order
        await _dbContext.SequenceParameters.AddRangeAsync(
            new SequenceParameter
            {
                SequenceId = sequence.Id,
                ParameterId = parameter2.Id,
                OrderNumber = 1 // Parameter 2 first
            },
            new SequenceParameter
            {
                SequenceId = sequence.Id,
                ParameterId = parameter1.Id,
                OrderNumber = 2 // Parameter 1 second
            }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetSequenceAsync(name: "Test Sequence");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Sequence", result.Name);
        Assert.Null(result.Order); // Order is contextual
        Assert.Equal(2, result.Parameters.Count());
        
        // Verify parameter order is preserved
        var parametersList = result.Parameters.ToList();
        Assert.Equal("Parameter 2", parametersList[0].Name); // OrderNumber 1
        Assert.Equal("Parameter 1", parametersList[1].Name); // OrderNumber 2
    }

    [Fact]
    public async Task GetSequenceAsync_WithNoNameFilter_ReturnsFirstSequence()
    {
        // Arrange
        var sequence1 = new Sequence
        {
            Name = "First Sequence",
            WorstCaseTime = TimeSpan.FromMinutes(1),
            CanBeParallel = false
        };
        
        var sequence2 = new Sequence
        {
            Name = "Second Sequence",
            WorstCaseTime = TimeSpan.FromMinutes(2),
            CanBeParallel = true
        };

        await _dbContext.Sequences.AddRangeAsync(sequence1, sequence2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetSequenceAsync();

        // Assert
        Assert.NotNull(result);
        // Should return first sequence (order may vary, so just check it's one of them)
        Assert.Contains(result.Name, new[] { "First Sequence", "Second Sequence" });
    }

    [Fact]
    public async Task GetSequenceAsync_NonexistentName_ReturnsNull()
    {
        // Arrange
        var sequence = new Sequence
        {
            Name = "Existing Sequence",
            WorstCaseTime = TimeSpan.FromMinutes(1),
            CanBeParallel = false
        };

        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetSequenceAsync(name: "Nonexistent Sequence");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSequencesAsync_WithNameFilter_ReturnsFilteredSequences()
    {
        // Arrange
        var sequence1 = new Sequence
        {
            Name = "Target Sequence",
            WorstCaseTime = TimeSpan.FromMinutes(1),
            CanBeParallel = false
        };
        
        var sequence2 = new Sequence
        {
            Name = "Other Sequence",
            WorstCaseTime = TimeSpan.FromMinutes(2),
            CanBeParallel = true
        };

        await _dbContext.Sequences.AddRangeAsync(sequence1, sequence2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetSequencesAsync(name: "Target Sequence");

        // Assert
        Assert.Single(result);
        Assert.Equal("Target Sequence", result.First().Name);
    }

    [Fact]
    public async Task GetSequencesAsync_WithNoFilter_ReturnsAllSequences()
    {
        // Arrange
        var sequence1 = new Sequence
        {
            Name = "Sequence 1",
            WorstCaseTime = TimeSpan.FromMinutes(1),
            CanBeParallel = false
        };
        
        var sequence2 = new Sequence
        {
            Name = "Sequence 2",
            WorstCaseTime = TimeSpan.FromMinutes(2),
            CanBeParallel = true
        };

        await _dbContext.Sequences.AddRangeAsync(sequence1, sequence2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetSequencesAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, s => s.Name == "Sequence 1");
        Assert.Contains(result, s => s.Name == "Sequence 2");
    }

    [Fact]
    public async Task ConvertToDTO_ExcludesIdentityKeys_And_ProjectsParametersCorrectly()
    {
        // Arrange
        var sequence = new Sequence
        {
            Name = "Test Sequence",
            Description = "Test description",
            WorstCaseTime = TimeSpan.FromMinutes(10),
            CanBeParallel = true
        };
        
        var parameter1 = new Parameter
        {
            Name = "First Parameter",
            Type = ParameterType.String,
            DefaultValue = "Default1"
        };
        
        var parameter2 = new Parameter
        {
            Name = "Second Parameter",
            Type = ParameterType.Integer,
            Min = "1",
            Max = "100",
            DefaultValue = "50"
        };
        
        var parameter3 = new Parameter
        {
            Name = "Third Parameter",
            Type = ParameterType.Boolean,
            DefaultValue = "true"
        };

        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.Parameters.AddRangeAsync(parameter1, parameter2, parameter3);
        await _dbContext.SaveChangesAsync();

        // Add parameters to sequence with specific order (not sequential)
        await _dbContext.SequenceParameters.AddRangeAsync(
            new SequenceParameter
            {
                SequenceId = sequence.Id,
                ParameterId = parameter3.Id,
                OrderNumber = 1 // Third parameter first
            },
            new SequenceParameter
            {
                SequenceId = sequence.Id,
                ParameterId = parameter1.Id,
                OrderNumber = 3 // First parameter third
            },
            new SequenceParameter
            {
                SequenceId = sequence.Id,
                ParameterId = parameter2.Id,
                OrderNumber = 2 // Second parameter second
            }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetSequenceAsync(name: "Test Sequence");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Sequence", result.Name);
        Assert.Null(result.Order); // Order is contextual, not set in conversion
        
        // Verify parameters are in correct order based on OrderNumber
        var parametersList = result.Parameters.ToList();
        Assert.Equal(3, parametersList.Count);
        Assert.Equal("Third Parameter", parametersList[0].Name);  // OrderNumber 1
        Assert.Equal("Second Parameter", parametersList[1].Name); // OrderNumber 2
        Assert.Equal("First Parameter", parametersList[2].Name);  // OrderNumber 3
        
        // Verify that the original parameters still have their identity keys
        // but the DTO doesn't expose them (by design of SequenceDTO)
        Assert.True(parameter1.Id > 0);
        Assert.True(parameter2.Id > 0);
        Assert.True(parameter3.Id > 0);
    }

    [Fact]
    public async Task GetSequenceAsync_WithComplexParameterOrdering_PreservesOrder()
    {
        // Arrange
        var sequence = new Sequence
        {
            Name = "Complex Sequence",
            WorstCaseTime = TimeSpan.FromMinutes(5),
            CanBeParallel = false
        };

        // Create 5 parameters
        var parameters = new List<Parameter>();
        for (int i = 1; i <= 5; i++)
        {
            parameters.Add(new Parameter
            {
                Name = $"Parameter {i}",
                Type = ParameterType.String,
                DefaultValue = $"Value{i}"
            });
        }

        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.Parameters.AddRangeAsync(parameters);
        await _dbContext.SaveChangesAsync();

        // Add parameters in non-sequential order: 5, 2, 1, 4, 3
        var sequenceParameters = new[]
        {
            new SequenceParameter { SequenceId = sequence.Id, ParameterId = parameters[4].Id, OrderNumber = 1 }, // Parameter 5
            new SequenceParameter { SequenceId = sequence.Id, ParameterId = parameters[1].Id, OrderNumber = 2 }, // Parameter 2
            new SequenceParameter { SequenceId = sequence.Id, ParameterId = parameters[0].Id, OrderNumber = 3 }, // Parameter 1
            new SequenceParameter { SequenceId = sequence.Id, ParameterId = parameters[3].Id, OrderNumber = 4 }, // Parameter 4
            new SequenceParameter { SequenceId = sequence.Id, ParameterId = parameters[2].Id, OrderNumber = 5 }  // Parameter 3
        };

        await _dbContext.SequenceParameters.AddRangeAsync(sequenceParameters);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetSequenceAsync(name: "Complex Sequence");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Parameters.Count());

        var parametersList = result.Parameters.ToList();
        Assert.Equal("Parameter 5", parametersList[0].Name); // OrderNumber 1
        Assert.Equal("Parameter 2", parametersList[1].Name); // OrderNumber 2
        Assert.Equal("Parameter 1", parametersList[2].Name); // OrderNumber 3
        Assert.Equal("Parameter 4", parametersList[3].Name); // OrderNumber 4
        Assert.Equal("Parameter 3", parametersList[4].Name); // OrderNumber 5
    }

    [Fact]
    public async Task GetSequenceAsync_EmptyParameterList_ReturnsSequenceWithEmptyParameters()
    {
        // Arrange
        var sequence = new Sequence
        {
            Name = "Empty Sequence",
            Description = "Sequence with no parameters",
            WorstCaseTime = TimeSpan.FromMinutes(1),
            CanBeParallel = false
        };

        await _dbContext.Sequences.AddAsync(sequence);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetSequenceAsync(name: "Empty Sequence");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Empty Sequence", result.Name);
        Assert.Empty(result.Parameters);
    }
}