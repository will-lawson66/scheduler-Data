using System.Text.Json;
using Instrument.Data.DTOs;
using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;
using Instrument.Data.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Instrument.Data.UT;

public class SequenceGroupImportExportServiceTests
{
    private readonly Mock<ISequenceGroupService> _mockSequenceGroupService;
    private readonly Mock<ISequenceService> _mockSequenceService;
    private readonly Mock<ILogger<SequenceGroupImportExportService>> _mockLogger;
    private readonly SequenceGroupImportExportService _service;

    public SequenceGroupImportExportServiceTests()
    {
        _mockSequenceGroupService = new Mock<ISequenceGroupService>();
        _mockSequenceService = new Mock<ISequenceService>();
        _mockLogger = new Mock<ILogger<SequenceGroupImportExportService>>();
        
        _service = new SequenceGroupImportExportService(
            _mockSequenceGroupService.Object,
            _mockSequenceService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExportSequenceGroupsToJsonAsync_WithValidData_ReturnsJsonWithOrderPreserved()
    {
        // Arrange
        var sequenceGroups = new List<SequenceGroupDTO>
        {
            new()
            {
                Name = "Test Group",
                Technology = Technology.ImmunoCap,
                Sequences = new[]
                {
                    new Sequence 
                    { 
                        Name = "First Sequence", 
                        WorstCaseTime = TimeSpan.FromMinutes(1),
                        Description = "First in order",
                        CanBeParallel = false
                    },
                    new Sequence 
                    { 
                        Name = "Second Sequence", 
                        WorstCaseTime = TimeSpan.FromMinutes(2),
                        Description = "Second in order", 
                        CanBeParallel = true
                    },
                    new Sequence 
                    { 
                        Name = "Third Sequence", 
                        WorstCaseTime = TimeSpan.FromMinutes(3),
                        Description = "Third in order",
                        CanBeParallel = false
                    }
                }
            }
        };

        _mockSequenceGroupService
            .Setup(s => s.GetSequenceGroupsAsync(null, null))
            .ReturnsAsync(sequenceGroups);

        // Act
        var json = await _service.ExportSequenceGroupsToJsonAsync();

        // Assert
        Assert.NotNull(json);
        Assert.NotEmpty(json);

        // Verify JSON can be deserialized and order is preserved
        var deserializedGroups = JsonSerializer.Deserialize<SequenceGroupDTO[]>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(deserializedGroups);
        Assert.Single(deserializedGroups);
        
        var group = deserializedGroups[0];
        Assert.Equal("Test Group", group.Name);
        Assert.Equal(Technology.ImmunoCap, group.Technology);
        Assert.Equal(3, group.Sequences.Count());

        // Verify sequence order is preserved
        var sequences = group.Sequences.ToArray();
        Assert.Equal("First Sequence", sequences[0].Name);
        Assert.Equal("Second Sequence", sequences[1].Name);
        Assert.Equal("Third Sequence", sequences[2].Name);
        
        // Verify TimeSpan serialization
        Assert.Equal(TimeSpan.FromMinutes(1), sequences[0].WorstCaseTime);
        Assert.Equal(TimeSpan.FromMinutes(2), sequences[1].WorstCaseTime);
        Assert.Equal(TimeSpan.FromMinutes(3), sequences[2].WorstCaseTime);
    }

    [Fact]
    public async Task ImportSequenceGroupsFromJsonAsync_WithValidJson_PreservesSequenceOrder()
    {
        // Arrange
        var json = @"[
            {
                ""name"": ""Test Import Group"",
                ""technology"": ""Elia"",
                ""sequences"": [
                    {
                        ""name"": ""Setup Sequence"",
                        ""description"": ""Initial setup"",
                        ""worstCaseTime"": ""00:01:30"",
                        ""canBeParallel"": false
                    },
                    {
                        ""name"": ""Process Sequence"",
                        ""description"": ""Main processing"",
                        ""worstCaseTime"": ""00:05:00"",
                        ""canBeParallel"": true
                    },
                    {
                        ""name"": ""Cleanup Sequence"",
                        ""description"": ""Final cleanup"",
                        ""worstCaseTime"": ""00:00:45"",
                        ""canBeParallel"": false
                    }
                ]
            }
        ]";

        // Mock that group doesn't exist
        _mockSequenceGroupService
            .Setup(s => s.GetSequenceGroupAsync("Test Import Group", null))
            .ReturnsAsync((SequenceGroupDTO?)null);

        // Mock sequence creation
        var sequenceId = 1;
        _mockSequenceService
            .Setup(s => s.GetAllSequencesAsync())
            .ReturnsAsync(new List<Sequence>());
        
        _mockSequenceService
            .Setup(s => s.CreateSequenceAsync(It.IsAny<Sequence>()))
            .ReturnsAsync((Sequence seq) => seq with { Id = sequenceId++ });

        // Mock group creation
        var createdGroup = new SequenceGroup 
        { 
            Id = 1, 
            Name = "Test Import Group", 
            Technology = Technology.Elia 
        };
        _mockSequenceGroupService
            .Setup(s => s.CreateSequenceGroupAsync(It.IsAny<SequenceGroup>()))
            .ReturnsAsync(createdGroup);

        _mockSequenceGroupService
            .Setup(s => s.AddSequenceToSequenceGroupAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ImportSequenceGroupsFromJsonAsync(json);

        // Assert
        Assert.True(result.Success, $"Import failed: {string.Join(", ", result.Errors)}");
        Assert.Equal(1, result.GroupsImported);
        Assert.Equal(3, result.TotalSequencesImported);
        Assert.Empty(result.Errors);

        // Verify sequences were added in correct order
        _mockSequenceGroupService.Verify(
            s => s.AddSequenceToSequenceGroupAsync(1, 1, 1), // Setup Sequence, order 1
            Times.Once);
        _mockSequenceGroupService.Verify(
            s => s.AddSequenceToSequenceGroupAsync(1, 2, 2), // Process Sequence, order 2
            Times.Once);
        _mockSequenceGroupService.Verify(
            s => s.AddSequenceToSequenceGroupAsync(1, 3, 3), // Cleanup Sequence, order 3
            Times.Once);
    }

    [Fact]
    public async Task ImportSequenceGroupsFromJsonAsync_WithInvalidJson_ReturnsErrors()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var result = await _service.ImportSequenceGroupsFromJsonAsync(invalidJson);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Invalid JSON format"));
        Assert.Equal(0, result.GroupsImported);
    }

    [Fact]
    public async Task ImportSequenceGroupsFromJsonAsync_WithExistingGroup_SkipsWhenReplaceExistingFalse()
    {
        // Arrange
        var json = @"[
            {
                ""name"": ""Existing Group"",
                ""technology"": ""ImmunoCap"",
                ""sequences"": [
                    {
                        ""name"": ""Test Sequence"",
                        ""worstCaseTime"": ""00:01:00"",
                        ""canBeParallel"": false
                    }
                ]
            }
        ]";

        // Mock that group already exists
        _mockSequenceGroupService
            .Setup(s => s.GetSequenceGroupAsync("Existing Group", null))
            .ReturnsAsync(new SequenceGroupDTO
            {
                Name = "Existing Group",
                Technology = Technology.ImmunoCap,
                Sequences = new List<Sequence>()
            });

        // Act
        var result = await _service.ImportSequenceGroupsFromJsonAsync(json, replaceExisting: false);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.GroupsImported);
        Assert.Equal(1, result.GroupsSkipped);
        Assert.Contains(result.Warnings, w => w.Contains("already exists"));
    }

    [Fact]
    public async Task ExportSequenceGroupsToFileAsync_CreatesFileWithCorrectContent()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        
        try
        {
            var sequenceGroups = new List<SequenceGroupDTO>
            {
                new()
                {
                    Name = "File Export Test",
                    Technology = Technology.ImmunoCapViewAllergy,
                    Sequences = new[]
                    {
                        new Sequence 
                        { 
                            Name = "Export Sequence", 
                            WorstCaseTime = TimeSpan.FromMinutes(2),
                            CanBeParallel = true
                        }
                    }
                }
            };

            _mockSequenceGroupService
                .Setup(s => s.GetSequenceGroupsAsync(null, null))
                .ReturnsAsync(sequenceGroups);

            // Act
            var result = await _service.ExportSequenceGroupsToFileAsync(tempFile);

            // Assert
            Assert.True(result.Success, $"Export failed: {string.Join(", ", result.Errors)}");
            Assert.Equal(1, result.GroupsExported);
            Assert.Equal(1, result.TotalSequencesExported);
            Assert.True(result.FileSizeBytes > 0);

            // Verify file was created and contains correct data
            Assert.True(File.Exists(tempFile));
            var fileContent = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("File Export Test", fileContent);
            Assert.Contains("ImmunoCapViewAllergy", fileContent);
            Assert.Contains("Export Sequence", fileContent);

            // Verify JSON structure
            var deserializedGroups = JsonSerializer.Deserialize<SequenceGroupDTO[]>(fileContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            Assert.NotNull(deserializedGroups);
            Assert.Single(deserializedGroups);
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
    public async Task ImportSequenceGroupsFromFileAsync_WithNonExistentFile_ReturnsError()
    {
        // Arrange
        var nonExistentFile = "C:\\NonExistent\\File.json";

        // Act
        var result = await _service.ImportSequenceGroupsFromFileAsync(nonExistentFile);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("File not found"));
    }

    [Fact]
    public void ImportResult_Summary_ReflectsCorrectStatus()
    {
        // Arrange & Act
        var successResult = new ImportResult
        {
            Success = true,
            GroupsImported = 5,
            GroupsReplaced = 2,
            GroupsSkipped = 1,
            TotalSequencesImported = 15
        };

        var failureResult = new ImportResult
        {
            Success = false,
            Errors = new List<string> { "Error 1", "Error 2" },
            Warnings = new List<string> { "Warning 1" }
        };

        // Assert
        Assert.Contains("Import successful", successResult.Summary);
        Assert.Contains("5 groups imported", successResult.Summary);
        Assert.Contains("2 replaced", successResult.Summary);
        Assert.Contains("1 skipped", successResult.Summary);
        Assert.Contains("15 sequences total", successResult.Summary);

        Assert.Contains("Import failed", failureResult.Summary);
        Assert.Contains("2 errors", failureResult.Summary);
        Assert.Contains("1 warnings", failureResult.Summary);
    }
}