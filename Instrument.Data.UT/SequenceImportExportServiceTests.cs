using System.Text.Json;
using Instrument.Data.DTOs;
using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;
using Instrument.Data.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Instrument.Data.UT;

public class SequenceImportExportServiceTests
{
    private readonly Mock<ISequenceService> _mockSequenceService;
    private readonly Mock<IParameterService> _mockParameterService;
    private readonly Mock<ILogger<SequenceImportExportService>> _mockLogger;
    private readonly SequenceImportExportService _service;

    public SequenceImportExportServiceTests()
    {
        _mockSequenceService = new Mock<ISequenceService>();
        _mockParameterService = new Mock<IParameterService>();
        _mockLogger = new Mock<ILogger<SequenceImportExportService>>();
        
        _service = new SequenceImportExportService(
            _mockSequenceService.Object,
            _mockParameterService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExportSequencesToJsonAsync_WithValidData_ReturnsJsonWithParameterOrderPreserved()
    {
        // Arrange
        var sequences = new List<SequenceDTO>
        {
            new()
            {
                Name = "Test Sequence",
                Order = null,
                Parameters = new[]
                {
                    new Parameter 
                    { 
                        Name = "First Parameter", 
                        Type = ParameterType.String,
                        DefaultValue = "Value1"
                    },
                    new Parameter 
                    { 
                        Name = "Second Parameter", 
                        Type = ParameterType.Integer,
                        Min = "1",
                        Max = "100", 
                        DefaultValue = "50"
                    },
                    new Parameter 
                    { 
                        Name = "Third Parameter", 
                        Type = ParameterType.Boolean,
                        DefaultValue = "true"
                    }
                }
            }
        };

        _mockSequenceService
            .Setup(s => s.GetSequencesAsync(null))
            .ReturnsAsync(sequences);

        // Act
        var json = await _service.ExportSequencesToJsonAsync();

        // Assert
        Assert.NotNull(json);
        Assert.NotEmpty(json);

        // Verify JSON can be deserialized and parameter order is preserved
        var deserializedSequences = JsonSerializer.Deserialize<SequenceDTO[]>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(deserializedSequences);
        Assert.Single(deserializedSequences);
        
        var sequence = deserializedSequences[0];
        Assert.Equal("Test Sequence", sequence.Name);
        Assert.Equal(3, sequence.Parameters.Count());

        // Verify parameter order is preserved
        var parameters = sequence.Parameters.ToArray();
        Assert.Equal("First Parameter", parameters[0].Name);
        Assert.Equal("Second Parameter", parameters[1].Name);
        Assert.Equal("Third Parameter", parameters[2].Name);
        
        // Verify parameter properties
        Assert.Equal(ParameterType.String, parameters[0].Type);
        Assert.Equal(ParameterType.Integer, parameters[1].Type);
        Assert.Equal(ParameterType.Boolean, parameters[2].Type);
    }

    [Fact]
    public async Task ImportSequencesFromJsonAsync_WithValidJson_PreservesParameterOrder()
    {
        // Arrange
        var json = @"[
            {
                ""name"": ""Test Import Sequence"",
                ""order"": null,
                ""parameters"": [
                    {
                        ""name"": ""Setup Parameter"",
                        ""type"": 0,
                        ""defaultValue"": ""setup_value""
                    },
                    {
                        ""name"": ""Process Parameter"",
                        ""type"": 1,
                        ""min"": ""1"",
                        ""max"": ""10"",
                        ""defaultValue"": ""5""
                    },
                    {
                        ""name"": ""Cleanup Parameter"",
                        ""type"": 2,
                        ""defaultValue"": ""false""
                    }
                ]
            }
        ]";

        // Mock that sequence doesn't exist
        _mockSequenceService
            .Setup(s => s.GetSequenceAsync("Test Import Sequence"))
            .ReturnsAsync((SequenceDTO?)null);

        // Mock parameter creation
        var parameterId = 1;
        _mockParameterService
            .Setup(s => s.GetAllParametersAsync())
            .ReturnsAsync(new List<Parameter>());
        
        _mockParameterService
            .Setup(s => s.CreateParameterAsync(It.IsAny<Parameter>()))
            .ReturnsAsync((Parameter param) => param with { Id = parameterId++ });

        // Mock sequence creation
        var createdSequence = new Sequence 
        { 
            Id = 1, 
            Name = "Test Import Sequence",
            WorstCaseTime = TimeSpan.FromMinutes(1),
            CanBeParallel = false
        };
        _mockSequenceService
            .Setup(s => s.CreateSequenceAsync(It.IsAny<Sequence>()))
            .ReturnsAsync(createdSequence);

        _mockSequenceService
            .Setup(s => s.AddParameterToSequenceAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ImportSequencesFromJsonAsync(json);

        // Assert
        Assert.True(result.Success, $"Import failed: {string.Join(", ", result.Errors)}");
        Assert.Equal(1, result.SequencesImported);
        Assert.Equal(3, result.TotalParametersImported);
        Assert.Empty(result.Errors);

        // Verify parameters were added in correct order
        _mockSequenceService.Verify(
            s => s.AddParameterToSequenceAsync(1, 1, 1), // Setup Parameter, order 1
            Times.Once);
        _mockSequenceService.Verify(
            s => s.AddParameterToSequenceAsync(2, 1, 2), // Process Parameter, order 2
            Times.Once);
        _mockSequenceService.Verify(
            s => s.AddParameterToSequenceAsync(3, 1, 3), // Cleanup Parameter, order 3
            Times.Once);
    }

    [Fact]
    public async Task ImportSequencesFromJsonAsync_WithInvalidJson_ReturnsErrors()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var result = await _service.ImportSequencesFromJsonAsync(invalidJson);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Invalid JSON format"));
        Assert.Equal(0, result.SequencesImported);
    }

    [Fact]
    public async Task ImportSequencesFromJsonAsync_WithExistingSequence_SkipsWhenReplaceExistingFalse()
    {
        // Arrange
        var json = @"[
            {
                ""name"": ""Existing Sequence"",
                ""parameters"": [
                    {
                        ""name"": ""Test Parameter"",
                        ""type"": 0,
                        ""defaultValue"": ""value""
                    }
                ]
            }
        ]";

        // Mock that sequence already exists
        _mockSequenceService
            .Setup(s => s.GetSequenceAsync("Existing Sequence"))
            .ReturnsAsync(new SequenceDTO
            {
                Name = "Existing Sequence",
                Parameters = new List<Parameter>()
            });

        // Act
        var result = await _service.ImportSequencesFromJsonAsync(json, replaceExisting: false);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.SequencesImported);
        Assert.Equal(1, result.SequencesSkipped);
        Assert.Contains(result.Warnings, w => w.Contains("already exists"));
    }

    [Fact]
    public async Task ExportSequencesToFileAsync_CreatesFileWithCorrectContent()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        
        try
        {
            var sequences = new List<SequenceDTO>
            {
                new()
                {
                    Name = "File Export Test Sequence",
                    Order = null,
                    Parameters = new[]
                    {
                        new Parameter 
                        { 
                            Name = "Export Parameter", 
                            Type = ParameterType.String,
                            DefaultValue = "export_value"
                        }
                    }
                }
            };

            _mockSequenceService
                .Setup(s => s.GetSequencesAsync(null))
                .ReturnsAsync(sequences);

            // Act
            var result = await _service.ExportSequencesToFileAsync(tempFile);

            // Assert
            Assert.True(result.Success, $"Export failed: {string.Join(", ", result.Errors)}");
            Assert.Equal(1, result.SequencesExported);
            Assert.Equal(1, result.TotalParametersExported);
            Assert.True(result.FileSizeBytes > 0);

            // Verify file was created and contains correct data
            Assert.True(File.Exists(tempFile));
            var fileContent = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("File Export Test Sequence", fileContent);
            Assert.Contains("Export Parameter", fileContent);

            // Verify JSON structure
            var deserializedSequences = JsonSerializer.Deserialize<SequenceDTO[]>(fileContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            Assert.NotNull(deserializedSequences);
            Assert.Single(deserializedSequences);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ImportSequencesFromFileAsync_WithNonExistentFile_ReturnsError()
    {
        // Arrange
        var nonExistentFile = "C:\\NonExistent\\File.json";

        // Act
        var result = await _service.ImportSequencesFromFileAsync(nonExistentFile);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("File not found"));
    }

    [Fact]
    public void SequenceImportResult_Summary_ReflectsCorrectStatus()
    {
        // Arrange & Act
        var successResult = new SequenceImportResult
        {
            Success = true,
            SequencesImported = 3,
            SequencesReplaced = 1,
            SequencesSkipped = 2,
            TotalParametersImported = 8
        };

        var failureResult = new SequenceImportResult
        {
            Success = false,
            Errors = new List<string> { "Error 1", "Error 2" },
            Warnings = new List<string> { "Warning 1" }
        };

        // Assert
        Assert.Contains("Import successful", successResult.Summary);
        Assert.Contains("3 sequences imported", successResult.Summary);
        Assert.Contains("1 replaced", successResult.Summary);
        Assert.Contains("2 skipped", successResult.Summary);
        Assert.Contains("8 parameters total", successResult.Summary);

        Assert.Contains("Import failed", failureResult.Summary);
        Assert.Contains("2 errors", failureResult.Summary);
        Assert.Contains("1 warnings", failureResult.Summary);
    }

    [Fact]
    public async Task ImportSequencesFromJsonAsync_WithComplexParameterOrder_PreservesOrder()
    {
        // Arrange - JSON with specific parameter order
        var json = @"[
            {
                ""name"": ""Complex Sequence"",
                ""parameters"": [
                    {
                        ""name"": ""Alpha Parameter"",
                        ""type"": 0,
                        ""defaultValue"": ""alpha""
                    },
                    {
                        ""name"": ""Beta Parameter"",
                        ""type"": 1,
                        ""min"": ""0"",
                        ""max"": ""100"",
                        ""defaultValue"": ""50""
                    },
                    {
                        ""name"": ""Gamma Parameter"",
                        ""type"": 2,
                        ""defaultValue"": ""true""
                    },
                    {
                        ""name"": ""Delta Parameter"",
                        ""type"": 0,
                        ""defaultValue"": ""delta""
                    }
                ]
            }
        ]";

        // Mock setup
        _mockSequenceService
            .Setup(s => s.GetSequenceAsync("Complex Sequence"))
            .ReturnsAsync((SequenceDTO?)null);

        var parameterId = 1;
        _mockParameterService
            .Setup(s => s.GetAllParametersAsync())
            .ReturnsAsync(new List<Parameter>());
        
        _mockParameterService
            .Setup(s => s.CreateParameterAsync(It.IsAny<Parameter>()))
            .ReturnsAsync((Parameter param) => param with { Id = parameterId++ });

        var createdSequence = new Sequence 
        { 
            Id = 1, 
            Name = "Complex Sequence",
            WorstCaseTime = TimeSpan.FromMinutes(1),
            CanBeParallel = false
        };
        _mockSequenceService
            .Setup(s => s.CreateSequenceAsync(It.IsAny<Sequence>()))
            .ReturnsAsync(createdSequence);

        _mockSequenceService
            .Setup(s => s.AddParameterToSequenceAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ImportSequencesFromJsonAsync(json);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(4, result.TotalParametersImported);

        // Verify parameters were added in the exact order from JSON
        _mockSequenceService.Verify(
            s => s.AddParameterToSequenceAsync(1, 1, 1), // Alpha Parameter, order 1
            Times.Once);
        _mockSequenceService.Verify(
            s => s.AddParameterToSequenceAsync(2, 1, 2), // Beta Parameter, order 2
            Times.Once);
        _mockSequenceService.Verify(
            s => s.AddParameterToSequenceAsync(3, 1, 3), // Gamma Parameter, order 3
            Times.Once);
        _mockSequenceService.Verify(
            s => s.AddParameterToSequenceAsync(4, 1, 4), // Delta Parameter, order 4
            Times.Once);
    }
}