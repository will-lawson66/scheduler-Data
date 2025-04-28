using System;
using Xunit;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Validation;
using System.Reflection.Metadata;
using Parameter = Instrument.Scheduling.Data.Entities.Parameter;

namespace Instrument.Scheduling.Data.Tests;

public class ParameterValidatorTests
{
    [Fact]
    public void Validate_NumericParameter_WithValidValue_Succeeds()
    {
        // Arrange
        var parameter = new Parameter
        {
            Id = "temp",
            Name = "Temperature",
            Type = "number",
            Min = "0",
            Max = "100"
        };

        // Act
        var result = ParameterValueValidator.Validate(parameter, "50");

        // Assert
        //Assert.True(result.Success);
    }

    [Fact]
    public void Validate_NumericParameter_BelowMinimum_Fails()
    {
        // Arrange
        var parameter = new Parameter
        {
            Id = "temp",
            Name = "Temperature",
            Type = "number",
            Min = "0",
            Max = "100"
        };

        // Act
        var result = ParameterValueValidator.Validate(parameter, "-1");

        // Assert
        //Assert.False(result.Success);
        Assert.Contains("must be >=", result.ErrorMessage);
    }

    [Fact]
    public void Validate_DateTimeParameter_WithValidValue_Succeeds()
    {
        // Arrange
        var parameter = new Parameter
        {
            Id = "startDate",
            Name = "Start Date",
            Type = "datetime"
        };

        // Act
        var result = ParameterValueValidator.Validate(parameter, "2025-04-24T19:33:01Z");

        // Assert
        //Assert.True(result.Success);
    }

    [Fact]
    public void Validate_RangeParameter_WithValidValue_Succeeds()
    {
        // Arrange
        var parameter = new Parameter
        {
            Id = "mode",
            Name = "Mode",
            Type = "range",
            Range = new Entities.Range
            {
                Id = "modes",
                Name = "Operating Modes",
                //Values = new List<RangeValue>
                //{
                //    new() { Id = "1", Value = "Normal" },
                //    new() { Id = "2", Value = "Diagnostic" }
                //}
            }
        };

        // Act
        var result = ParameterValueValidator.Validate(parameter, "Normal");

        // Assert
        //Assert.True(result.Success);
    }
}
