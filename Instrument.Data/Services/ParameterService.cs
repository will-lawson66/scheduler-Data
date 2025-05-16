using Instrument.Data.Entities.Enums;
using Instrument.Data.Entities;
using Instrument.Data.Exceptions;
using Microsoft.Extensions.Logging;

namespace Instrument.Data.Services;

public class ParameterService : IParameterService
{
    private readonly ILogger<ParameterService> _logger;
    private readonly IParameterRepository _parameterRepository;

    public ParameterService(
        IParameterRepository parameterRepository,
        ILogger<ParameterService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parameterRepository = parameterRepository ?? throw new ArgumentNullException(nameof(parameterRepository));
    }

    public async Task<Parameter?> GetParameterByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving parameter with ID: {Id}", id);
        try
        {
            return await _parameterRepository.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving parameter with ID: {Id}", id);
            throw new StorageProviderException("GetParameter", ex);
        }
    }

    public async Task<Parameter> CreateParameterAsync(Parameter parameter)
    {
        if (parameter == null)
        {
            throw new ArgumentNullException(nameof(parameter));
        }

        _logger.LogInformation("Creating new parameter with Name: {Name}", parameter.Name);
        
        try
        {
            await _parameterRepository.AddAsync(parameter);
            await _parameterRepository.SaveChangesAsync();
            _logger.LogInformation("Successfully created parameter with ID: {Id}", parameter.Id);
            return parameter;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating parameter with ID: {Id}", parameter.Id);
            throw new StorageProviderException("CreateParameter", ex);
        }
    }

    public async Task UpdateParameterAsync(Parameter parameter)
    {
        if (parameter == null)
        {
            throw new ArgumentNullException(nameof(parameter));
        }

        _logger.LogInformation("Updating parameter with ID: {Id}, Name: {Name}", parameter.Id, parameter.Name);
        
        // Validate if the parameter exists
        var existingParameter = await _parameterRepository.GetByIdAsync(parameter.Id);
        if (existingParameter == null)
        {
            _logger.LogWarning("Parameter with ID {Id} does not exist", parameter.Id);
            throw new EntityNotFoundException("Parameter", parameter.Id);
        }

        try
        {
            await _parameterRepository.UpdateAsync(parameter);
            await _parameterRepository.SaveChangesAsync();
            _logger.LogInformation("Successfully updated parameter with ID: {Id}", parameter.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating parameter with ID: {Id}", parameter.Id);
            throw new StorageProviderException("UpdateParameter", ex);
        }
    }
    
    public async Task DeleteParameterAsync(int id)
    {
        _logger.LogInformation("Deleting parameter with ID: {Id}", id);
        
        // Validate if the parameter exists
        var existingParameter = await _parameterRepository.GetByIdAsync(id);
        if (existingParameter == null)
        {
            _logger.LogWarning("Parameter with ID {Id} does not exist", id);
            throw new EntityNotFoundException("Parameter", id);
        }

        try
        {
            await _parameterRepository.DeleteAsync(id);
            await _parameterRepository.SaveChangesAsync();
            _logger.LogInformation("Successfully deleted parameter with ID: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting parameter with ID: {Id}", id);
            throw new StorageProviderException("DeleteParameter", ex);
        }
    }

    public async Task<IEnumerable<Parameter>> GetAllParametersAsync()
    {
        _logger.LogInformation("Retrieving all parameters");
        try
        {
            return await _parameterRepository.GetAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all parameters");
            throw new StorageProviderException("GetAllParameters", ex);
        }
    }

    /// <inheritdoc />
    public void ValidateParameterValue(Parameter parameter, string value)
    {
        if (parameter == null)
        {
            throw new ArgumentNullException(nameof(parameter));
        }
            
        _logger.LogInformation("Validating value '{Value}' for parameter {ParameterId} of type {ParameterType}", 
            value, parameter.Id, parameter.Type);
            
        if (string.IsNullOrEmpty(value))
        {
            _logger.LogInformation("Value is null or empty for parameter {ParameterId}", parameter.Id);
            throw new ValidationException(parameter.Id, value, "Value cannot be null or empty");
        }
        
        switch (parameter.Type)
        {
            case ParameterType.IntegerType:
            case ParameterType.DecimalType:
                if (!decimal.TryParse(value, out var numValue))
                {
                    _logger.LogWarning("Value '{Value}' is not a valid number for parameter {ParameterId}", 
                        value, parameter.Id);
                    throw new ValidationException(parameter.Id, value, "Value is not a valid number");
                }
                
                if (!string.IsNullOrEmpty(parameter.Min) && 
                    decimal.TryParse(parameter.Min, out var minValue) &&
                    numValue < minValue)
                {
                    _logger.LogWarning("Value {Value} is less than minimum value {MinValue} for parameter {ParameterId}", 
                        numValue, minValue, parameter.Id);
                    throw new ValidationException(parameter.Id, value, $"Value must be greater than or equal to {minValue}");
                }
                
                if (!string.IsNullOrEmpty(parameter.Max) && 
                    decimal.TryParse(parameter.Max, out var maxValue) &&
                    numValue > maxValue)
                {
                    _logger.LogWarning("Value {Value} is greater than maximum value {MaxValue} for parameter {ParameterId}", 
                        numValue, maxValue, parameter.Id);
                    throw new ValidationException(parameter.Id, value, $"Value must be less than or equal to {maxValue}");
                }
                
                _logger.LogInformation("Value {Value} is valid for numeric parameter {ParameterId}", 
                    numValue, parameter.Id);
                break;
                
            case ParameterType.StringType:
                if (!string.IsNullOrEmpty(parameter.Min) && 
                    int.TryParse(parameter.Min, out var minLength) &&
                    value.Length < minLength)
                {
                    _logger.LogWarning("String length {Length} is less than minimum length {MinLength} for parameter {ParameterId}", 
                        value.Length, minLength, parameter.Id);
                    throw new ValidationException(parameter.Id, value, $"String length must be at least {minLength} characters");
                }
                
                if (!string.IsNullOrEmpty(parameter.Max) && 
                    int.TryParse(parameter.Max, out var maxLength) &&
                    value.Length > maxLength)
                {
                    _logger.LogWarning("String length {Length} is greater than maximum length {MaxLength} for parameter {ParameterId}", 
                        value.Length, maxLength, parameter.Id);
                    throw new ValidationException(parameter.Id, value, $"String length must be at most {maxLength} characters");
                }
                
                _logger.LogInformation("String value is valid for parameter {ParameterId}", parameter.Id);
                break;
                
            case ParameterType.BooleanType:
                if (!bool.TryParse(value, out _))
                {
                    _logger.LogWarning("Value '{Value}' is not a valid boolean for parameter {ParameterId}", 
                        value, parameter.Id);
                    throw new ValidationException(parameter.Id, value, "Value is not a valid boolean (true/false)");
                }
                
                _logger.LogInformation("Boolean value is valid for parameter {ParameterId}", parameter.Id);
                break;
                
            default:
                // For custom types, we still return true but don't perform any validation
                _logger.LogInformation("Custom type parameter {ParameterId} validation defaulting to success", 
                    parameter.Id);
                break;
        }
    }
    
    // Simple wrapper method that returns a boolean instead of throwing exceptions
    public bool TryValidateParameterValue(Parameter parameter, string value)
    {
        try
        {
            ValidateParameterValue(parameter, value);
            return true;
        }
        catch (ValidationException)
        {
            return false;
        }
    }
}
