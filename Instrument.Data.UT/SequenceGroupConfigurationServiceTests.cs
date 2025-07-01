using Instrument.Data.Configuration;
using Instrument.Data.DTOs;
using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;
using Instrument.Data.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Instrument.Data.UT;

public class SequenceGroupConfigurationServiceTests
{
    private readonly Mock<ILogger<SequenceGroupConfigurationService>> _mockLogger;

    public SequenceGroupConfigurationServiceTests()
    {
        _mockLogger = new Mock<ILogger<SequenceGroupConfigurationService>>();
    }

    [Fact]
    public void GetDefaultGroup_WithConfiguredDefault_ReturnsDefault()
    {
        // Arrange
        var defaultGroup = new SequenceGroupDTO
        {
            Name = "Default Test Group",
            Technology = Technology.ImmunoCap,
            Sequences = new[]
            {
                new Sequence
                {
                    Name = "Test Sequence",
                    WorstCaseTime = TimeSpan.FromMinutes(5)
                }
            }
        };

        var options = Options.Create(new SequenceGroupOptions
        {
            DefaultGroup = defaultGroup
        });

        var service = new SequenceGroupConfigurationService(options, _mockLogger.Object);

        // Act
        var result = service.GetDefaultGroup();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Default Test Group", result.Name);
        Assert.Equal(Technology.ImmunoCap, result.Technology);
        Assert.Single(result.Sequences);
    }

    [Fact]
    public void GetDefaultGroup_WithNoDefault_ReturnsNull()
    {
        // Arrange
        var options = Options.Create(new SequenceGroupOptions
        {
            DefaultGroup = null
        });

        var service = new SequenceGroupConfigurationService(options, _mockLogger.Object);

        // Act
        var result = service.GetDefaultGroup();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetConfiguredGroups_ReturnsAllGroups()
    {
        // Arrange
        var groups = new List<SequenceGroupDTO>
        {
            new()
            {
                Name = "Group 1",
                Technology = Technology.ImmunoCap,
                Sequences = Array.Empty<Sequence>()
            },
            new()
            {
                Name = "Group 2",
                Technology = Technology.Elia,
                Sequences = Array.Empty<Sequence>()
            }
        };

        var options = Options.Create(new SequenceGroupOptions
        {
            Groups = groups
        });

        var service = new SequenceGroupConfigurationService(options, _mockLogger.Object);

        // Act
        var result = service.GetConfiguredGroups();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, g => g.Name == "Group 1");
        Assert.Contains(result, g => g.Name == "Group 2");
    }

    [Fact]
    public void GetGroupsByTechnology_FiltersCorrectly()
    {
        // Arrange
        var groups = new List<SequenceGroupDTO>
        {
            new()
            {
                Name = "ImmunoCap Group",
                Technology = Technology.ImmunoCap,
                Sequences = Array.Empty<Sequence>()
            },
            new()
            {
                Name = "Elia Group",
                Technology = Technology.Elia,
                Sequences = Array.Empty<Sequence>()
            },
            new()
            {
                Name = "Another ImmunoCap Group",
                Technology = Technology.ImmunoCap,
                Sequences = Array.Empty<Sequence>()
            }
        };

        var options = Options.Create(new SequenceGroupOptions
        {
            Groups = groups
        });

        var service = new SequenceGroupConfigurationService(options, _mockLogger.Object);

        // Act
        var result = service.GetGroupsByTechnology(Technology.ImmunoCap);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, g => Assert.Equal(Technology.ImmunoCap, g.Technology));
        Assert.Contains(result, g => g.Name == "ImmunoCap Group");
        Assert.Contains(result, g => g.Name == "Another ImmunoCap Group");
    }

    [Fact]
    public void GetGroupByName_FindsCorrectGroup()
    {
        // Arrange
        var groups = new List<SequenceGroupDTO>
        {
            new()
            {
                Name = "Test Group",
                Technology = Technology.ImmunoCap,
                Sequences = Array.Empty<Sequence>()
            },
            new()
            {
                Name = "Other Group",
                Technology = Technology.Elia,
                Sequences = Array.Empty<Sequence>()
            }
        };

        var options = Options.Create(new SequenceGroupOptions
        {
            Groups = groups
        });

        var service = new SequenceGroupConfigurationService(options, _mockLogger.Object);

        // Act
        var result = service.GetGroupByName("Test Group");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Group", result.Name);
        Assert.Equal(Technology.ImmunoCap, result.Technology);
    }

    [Fact]
    public void GetGroupByName_CaseInsensitive_FindsGroup()
    {
        // Arrange
        var groups = new List<SequenceGroupDTO>
        {
            new()
            {
                Name = "Test Group",
                Technology = Technology.ImmunoCap,
                Sequences = Array.Empty<Sequence>()
            }
        };

        var options = Options.Create(new SequenceGroupOptions
        {
            Groups = groups
        });

        var service = new SequenceGroupConfigurationService(options, _mockLogger.Object);

        // Act
        var result = service.GetGroupByName("test group");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Group", result.Name);
    }

    [Fact]
    public void GetGroupByName_NotFound_ReturnsNull()
    {
        // Arrange
        var options = Options.Create(new SequenceGroupOptions
        {
            Groups = new List<SequenceGroupDTO>
            {
                new()
                {
                    Name = "Other Group",
                    Technology = Technology.Elia,
                    Sequences = Array.Empty<Sequence>()
                }
            }
        });

        var service = new SequenceGroupConfigurationService(options, _mockLogger.Object);

        // Act
        var result = service.GetGroupByName("Nonexistent Group");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ValidateConfiguration_WithValidData_ReturnsValid()
    {
        // Arrange
        var groups = new List<SequenceGroupDTO>
        {
            new()
            {
                Name = "Valid Group",
                Technology = Technology.ImmunoCap,
                Sequences = new[]
                {
                    new Sequence
                    {
                        Name = "Valid Sequence",
                        WorstCaseTime = TimeSpan.FromMinutes(5)
                    }
                }
            }
        };

        var options = Options.Create(new SequenceGroupOptions
        {
            DefaultGroup = groups[0],
            Groups = groups
        });

        var service = new SequenceGroupConfigurationService(options, _mockLogger.Object);

        // Act
        var result = service.ValidateConfiguration();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateConfiguration_WithEmptyName_ReturnsErrors()
    {
        // Arrange
        var groups = new List<SequenceGroupDTO>
        {
            new()
            {
                Name = "", // Invalid
                Technology = Technology.ImmunoCap,
                Sequences = new[]
                {
                    new Sequence
                    {
                        Name = "Valid Sequence",
                        WorstCaseTime = TimeSpan.FromMinutes(5)
                    }
                }
            }
        };

        var options = Options.Create(new SequenceGroupOptions
        {
            Groups = groups
        });

        var service = new SequenceGroupConfigurationService(options, _mockLogger.Object);

        // Act
        var result = service.ValidateConfiguration();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Name is required"));
    }

    [Fact]
    public void ValidateConfiguration_WithNoSequences_ReturnsWarnings()
    {
        // Arrange
        var groups = new List<SequenceGroupDTO>
        {
            new()
            {
                Name = "Valid Group",
                Technology = Technology.ImmunoCap,
                Sequences = Array.Empty<Sequence>() // No sequences
            }
        };

        var options = Options.Create(new SequenceGroupOptions
        {
            Groups = groups
        });

        var service = new SequenceGroupConfigurationService(options, _mockLogger.Object);

        // Act
        var result = service.ValidateConfiguration();

        // Assert
        Assert.True(result.IsValid); // Valid but with warnings
        Assert.Contains(result.Warnings, w => w.Contains("No sequences configured"));
    }

    [Fact]
    public void ValidateConfiguration_WithDuplicateNames_ReturnsErrors()
    {
        // Arrange
        var groups = new List<SequenceGroupDTO>
        {
            new()
            {
                Name = "Duplicate Group",
                Technology = Technology.ImmunoCap,
                Sequences = new[]
                {
                    new Sequence
                    {
                        Name = "Sequence 1",
                        WorstCaseTime = TimeSpan.FromMinutes(5)
                    }
                }
            },
            new()
            {
                Name = "Duplicate Group", // Duplicate name
                Technology = Technology.Elia,
                Sequences = new[]
                {
                    new Sequence
                    {
                        Name = "Sequence 2",
                        WorstCaseTime = TimeSpan.FromMinutes(3)
                    }
                }
            }
        };

        var options = Options.Create(new SequenceGroupOptions
        {
            Groups = groups
        });

        var service = new SequenceGroupConfigurationService(options, _mockLogger.Object);

        // Act
        var result = service.ValidateConfiguration();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Duplicate sequence group name found: 'Duplicate Group'"));
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidSequenceTime_ReturnsErrors()
    {
        // Arrange
        var groups = new List<SequenceGroupDTO>
        {
            new()
            {
                Name = "Test Group",
                Technology = Technology.ImmunoCap,
                Sequences = new[]
                {
                    new Sequence
                    {
                        Name = "Invalid Sequence",
                        WorstCaseTime = TimeSpan.Zero // Invalid time
                    }
                }
            }
        };

        var options = Options.Create(new SequenceGroupOptions
        {
            Groups = groups
        });

        var service = new SequenceGroupConfigurationService(options, _mockLogger.Object);

        // Act
        var result = service.ValidateConfiguration();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("WorstCaseTime must be greater than zero"));
    }

    [Fact]
    public void ValidateConfiguration_WithLongSequenceTime_ReturnsWarnings()
    {
        // Arrange
        var groups = new List<SequenceGroupDTO>
        {
            new()
            {
                Name = "Test Group",
                Technology = Technology.ImmunoCap,
                Sequences = new[]
                {
                    new Sequence
                    {
                        Name = "Long Sequence",
                        WorstCaseTime = TimeSpan.FromHours(25) // Very long time
                    }
                }
            }
        };

        var options = Options.Create(new SequenceGroupOptions
        {
            Groups = groups
        });

        var service = new SequenceGroupConfigurationService(options, _mockLogger.Object);

        // Act
        var result = service.ValidateConfiguration();

        // Assert
        Assert.True(result.IsValid); // Valid but with warnings
        Assert.Contains(result.Warnings, w => w.Contains("unusually long"));
    }
}