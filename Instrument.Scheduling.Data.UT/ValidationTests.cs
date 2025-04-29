using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Validation;
using System.ComponentModel.DataAnnotations;

namespace Instrument.Scheduling.Data.UT;
public class ValidationTests
{
    [Fact]
    public void Validate_NumericParameter_WithValidValue_Succeeds()
    {
        // Arrange
        var parameter = CreateNumericParameter();

        // Act
        var result = ParameterValueValidator.Validate(parameter, "50");

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }

    [Theory]
    [InlineData("-1", "must be >=")]
    [InlineData("101", "must be <=")]
    [InlineData("abc", "Invalid numeric")]
    public void Validate_NumericParameter_WithInvalidValues_Fails(string value, string expectedError)
    {
        // Arrange
        var parameter = CreateNumericParameter();

        // Act
        var result = ParameterValueValidator.Validate(parameter, value);

        // Assert
        Assert.Contains(expectedError, result.ErrorMessage);
    }

    [Theory]
    [InlineData("2025-04-24T19:33:01Z")]
    [InlineData("2025-04-24")]
    [InlineData("2025/04/24 19:33:01")]
    public void Validate_DateTimeParameter_WithValidValue_Succeeds(string dateValue)
    {
        // Arrange
        var parameter = new Parameter
        {
            Id = "startDate",
            Name = "Start Date",
            Type = "datetime"
        };

        // Act
        var result = ParameterValueValidator.Validate(parameter, dateValue);

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void Validate_DateTimeParameter_WithInvalidValue_Fails()
    {
        // Arrange
        var parameter = new Parameter
        {
            Id = "startDate",
            Name = "Start Date",
            Type = "datetime"
        };

        // Act
        var result = ParameterValueValidator.Validate(parameter, "not-a-date");

        // Assert
        Assert.Contains("Invalid date", result.ErrorMessage);
    }

    [Fact]
    public void Validate_RangeParameter_WithValidValue_Succeeds()
    {
        // Arrange
        var parameter = CreateRangeParameter();

        // Act
        var result = ParameterValueValidator.Validate(parameter, "Normal");

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void Validate_RangeParameter_WithInvalidValue_Fails()
    {
        // Arrange
        var parameter = CreateRangeParameter();

        // Act
        var result = ParameterValueValidator.Validate(parameter, "Turbo");

        // Assert
        Assert.Contains("not in allowed range", result.ErrorMessage);
    }

    // Helper methods for creating test parameters
    private Parameter CreateNumericParameter()
    {
        return new Parameter
        {
            Id = "temp",
            Name = "Temperature",
            Type = "number",
            Min = "0",
            Max = "100"
        };
    }

    private Parameter CreateRangeParameter()
    {
        return new Parameter
        {
            Id = "mode",
            Name = "Mode",
            Type = "range",
            Range = new Entities.Range
            {
                Id = "modes",
                Name = "Operating Modes",
                Values = new List<RangeValue>
            {
                new RangeValue
                {
                    Id = "1",
                    RangeId = "modes",
                    Value = "Normal",
                    Name = string.Empty
                },
                new RangeValue
                {
                    Id = "2",
                    RangeId = "modes",
                    Value = "Diagnostic",
                    Name = string.Empty
                }
            }
            }
        };
    }
}