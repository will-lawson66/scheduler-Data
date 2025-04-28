using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Instrument.Scheduling.Data.Entities;

namespace Instrument.Scheduling.Data.Validation;

public static class ParameterValueValidator
{
    public static ValidationResult Validate(Parameter parameter, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new ValidationResult("Value cannot be empty");

        switch (parameter.Type.ToLowerInvariant())
        {
            case "number":
                return ValidateNumeric(parameter, value);
            case "datetime":
                return ValidateDateTime(value);
            case "range":
                return ValidateRange(parameter, value);
            default:
                return ValidateString(parameter, value);
        }
    }

    private static ValidationResult ValidateNumeric(Parameter parameter, string value)
    {
        if (!decimal.TryParse(value, out var numericValue))
            return new ValidationResult("Invalid numeric value");

        if (parameter.Min != null && decimal.Parse(parameter.Min) > numericValue)
            return new ValidationResult($"Value must be >= {parameter.Min}");

        if (parameter.Max != null && decimal.Parse(parameter.Max) < numericValue)
            return new ValidationResult($"Value must be <= {parameter.Max}");

        return ValidationResult.Success!;
    }

    private static ValidationResult ValidateDateTime(string value)
    {
        if (!DateTime.TryParse(value, out _))
            return new ValidationResult("Invalid date/time value");

        return ValidationResult.Success!;
    }

    private static ValidationResult ValidateRange(Parameter parameter, string value)
    {
        if (parameter.Range == null)
            return new ValidationResult("Range not configured for parameter");

        if (!parameter.Range.Values.Any(rv => rv.Value == value))
            return new ValidationResult($"Value not in allowed range: {value}");

        return ValidationResult.Success!;
    }

    private static ValidationResult ValidateString(Parameter parameter, string value)
    {
        if (parameter.Max != null && int.TryParse(parameter.Max, out var maxLength))
        {
            if (value.Length > maxLength)
                return new ValidationResult($"Value length exceeds maximum of {maxLength}");
        }

        return ValidationResult.Success!;
    }
}

