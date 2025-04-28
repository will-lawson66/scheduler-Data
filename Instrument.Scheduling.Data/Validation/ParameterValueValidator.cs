using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Exceptions;

namespace Instrument.Scheduling.Data.Validation;

/// <summary>
/// Validates parameter values against their configured constraints
/// </summary>
public static class ParameterValueValidator
{
    /// <summary>
    /// Validates a parameter value against its configuration
    /// </summary>
    /// <param name="parameter">The parameter configuration</param>
    /// <param name="value">The value to validate</param>
    /// <returns>Validation result indicating success or failure</returns>
    /// <exception cref="ArgumentNullException">Thrown if parameter is null</exception>
    public static ValidationResult Validate(Parameter parameter, string value)
    {
        if (parameter == null)
            throw new ArgumentNullException(nameof(parameter));

        if (string.IsNullOrWhiteSpace(value) && !IsNullableParameter(parameter))
            return new ValidationResult("Value cannot be empty");

        return parameter.Type.ToLowerInvariant() switch
        {
            "number" => ValidateNumeric(parameter, value),
            "integer" => ValidateInteger(parameter, value),
            "datetime" => ValidateDateTime(parameter, value),
            "range" => ValidateRange(parameter, value),
            "boolean" => ValidateBoolean(value),
            _ => ValidateString(parameter, value)
        };
    }

    /// <summary>
    /// Validates a parameter value and throws an exception if validation fails
    /// </summary>
    /// <param name="parameter">The parameter configuration</param>
    /// <param name="value">The value to validate</param>
    /// <exception cref="ParameterValidationException">Thrown if validation fails</exception>
    public static void ValidateAndThrow(Parameter parameter, string value)
    {
        var result = Validate(parameter, value);
        if (result != ValidationResult.Success)
        {
            throw new ParameterValidationException(
                parameter.Id,
                value,
                result.ErrorMessage ?? "Validation failed"
            );
        }
    }

    private static bool IsNullableParameter(Parameter parameter)
    {
        // Determine if parameter is nullable based on configuration
        return parameter.DefaultValue == null &&
               (parameter.Min == null || parameter.Min == "null");
    }

    private static ValidationResult ValidateNumeric(Parameter parameter, string value)
    {
        if (!decimal.TryParse(value, out var numericValue))
            return new ValidationResult("Invalid numeric value");

        if (parameter.Min != null && decimal.TryParse(parameter.Min, out var min) && min > numericValue)
            return new ValidationResult($"Value must be >= {parameter.Min}");

        if (parameter.Max != null && decimal.TryParse(parameter.Max, out var max) && max < numericValue)
            return new ValidationResult($"Value must be <= {parameter.Max}");

        return ValidationResult.Success!;
    }

    private static ValidationResult ValidateInteger(Parameter parameter, string value)
    {
        if (!int.TryParse(value, out var intValue))
            return new ValidationResult("Invalid integer value");

        if (parameter.Min != null && int.TryParse(parameter.Min, out var min) && min > intValue)
            return new ValidationResult($"Value must be >= {parameter.Min}");

        if (parameter.Max != null && int.TryParse(parameter.Max, out var max) && max < intValue)
            return new ValidationResult($"Value must be <= {parameter.Max}");

        return ValidationResult.Success!;
    }

    private static ValidationResult ValidateDateTime(Parameter parameter, string value)
    {
        if (!DateTime.TryParse(value, out var dateValue))
            return new ValidationResult("Invalid date/time value");

        if (parameter.Min != null && DateTime.TryParse(parameter.Min, out var minDate) && minDate > dateValue)
            return new ValidationResult($"Date must be on or after {minDate:yyyy-MM-dd}");

        if (parameter.Max != null && DateTime.TryParse(parameter.Max, out var maxDate) && maxDate < dateValue)
            return new ValidationResult($"Date must be on or before {maxDate:yyyy-MM-dd}");

        return ValidationResult.Success!;
    }

    private static ValidationResult ValidateBoolean(string value)
    {
        if (!bool.TryParse(value, out _) &&
            !string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(value, "1", StringComparison.OrdinalIgnoreCase))
            return new ValidationResult("Invalid boolean value. Use 'true', 'false', '1', or '0'");

        return ValidationResult.Success!;
    }

    private static ValidationResult ValidateRange(Parameter parameter, string value)
    {
        if (parameter.Range == null)
            return new ValidationResult("Range not configured for parameter");

        if (!parameter.Range.Values.Any(rv => string.Equals(rv.Value, value, StringComparison.OrdinalIgnoreCase)))
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

        if (parameter.Format != null)
        {
            try
            {
                var regex = new System.Text.RegularExpressions.Regex(parameter.Format);
                if (!regex.IsMatch(value))
                    return new ValidationResult($"Value does not match required format");
            }
            catch (Exception ex)
            {
                // Invalid regex pattern in format - this is a configuration issue
                return new ValidationResult($"Invalid format configuration: {ex.Message}");
            }
        }

        return ValidationResult.Success!;
    }
}