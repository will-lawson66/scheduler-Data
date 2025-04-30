using Instrument.Scheduling.Data.Entities.Enums;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Interfaces;
using Microsoft.Extensions.Logging;

namespace Instrument.Scheduling.Data.Services;

public class ParameterService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ParameterService> _logger;

    public ParameterService(
        IUnitOfWork unitOfWork,
        ILogger<ParameterService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Parameter?> GetParameterAsync(string id)
    {
        _logger.LogInformation("Retrieving parameter with ID: {Id}", id);
        try
        {
            return await _unitOfWork.Parameters.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving parameter with ID: {Id}", id);
            throw new StorageProviderException("GetParameter", ex);
        }
    }

    public async Task CreateParameterAsync(Parameter parameter)
    {
        if (parameter == null)
            throw new ArgumentNullException(nameof(parameter));

        _logger.LogInformation("Creating new parameter with ID: {Id}, Name: {Name}", parameter.Id, parameter.Name);
        
        // Validate if a parameter with this ID already exists
        var existingParameter = await _unitOfWork.Parameters.GetByIdAsync(parameter.Id);
        if (existingParameter != null)
        {
            _logger.LogWarning("Parameter with ID {Id} already exists", parameter.Id);
            throw new SchedulerDataException($"Parameter with ID {parameter.Id} already exists");
        }

        try
        {
            await _unitOfWork.Parameters.AddAsync(parameter);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Successfully created parameter with ID: {Id}", parameter.Id);
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
            throw new ArgumentNullException(nameof(parameter));

        _logger.LogInformation("Updating parameter with ID: {Id}, Name: {Name}", parameter.Id, parameter.Name);
        
        // Validate if the parameter exists
        var existingParameter = await _unitOfWork.Parameters.GetByIdAsync(parameter.Id);
        if (existingParameter == null)
        {
            _logger.LogWarning("Parameter with ID {Id} does not exist", parameter.Id);
            throw new EntityNotFoundException("Parameter", parameter.Id);
        }

        try
        {
            await _unitOfWork.Parameters.UpdateAsync(parameter);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Successfully updated parameter with ID: {Id}", parameter.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating parameter with ID: {Id}", parameter.Id);
            throw new StorageProviderException("UpdateParameter", ex);
        }
    }

    public async Task DeleteParameterAsync(string id)
    {
        _logger.LogInformation("Deleting parameter with ID: {Id}", id);
        
        // Validate if the parameter exists
        var existingParameter = await _unitOfWork.Parameters.GetByIdAsync(id);
        if (existingParameter == null)
        {
            _logger.LogWarning("Parameter with ID {Id} does not exist", id);
            throw new EntityNotFoundException("Parameter", id);
        }

        try
        {
            await _unitOfWork.Parameters.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
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
            return await _unitOfWork.Parameters.GetAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all parameters");
            throw new StorageProviderException("GetAllParameters", ex);
        }
    }
    
    // Get parameters for a specific sequence
    public async Task<IEnumerable<Parameter>> GetParametersForSequenceAsync(string sequenceId)
    {
        _logger.LogInformation("Retrieving parameters for sequence ID: {SequenceId}", sequenceId);
        try
        {
            return await _unitOfWork.Parameters.GetParametersForSequenceAsync(sequenceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving parameters for sequence ID: {SequenceId}", sequenceId);
            throw new StorageProviderException("GetParametersForSequence", ex);
        }
    }
    
    // Add a parameter to a sequence
    public async Task AddParameterToSequenceAsync(string sequenceId, string parameterId, int orderNumber)
    {
        _logger.LogInformation("Adding parameter {ParameterId} to sequence {SequenceId} with order {OrderNumber}", 
            parameterId, sequenceId, orderNumber);
        
        // Validate parameter exists
        var parameter = await _unitOfWork.Parameters.GetByIdAsync(parameterId);
        if (parameter == null)
        {
            _logger.LogWarning("Parameter with ID {Id} does not exist", parameterId);
            throw new EntityNotFoundException("Parameter", parameterId);
        }
        
        // Validate sequence exists
        var sequence = await _unitOfWork.SequenceDefinitions.GetByIdAsync(sequenceId);
        if (sequence == null)
        {
            _logger.LogWarning("Sequence with ID {Id} does not exist", sequenceId);
            throw new EntityNotFoundException("Sequence", sequenceId);
        }

        try
        {
            await _unitOfWork.Parameters.AddParameterToSequenceAsync(sequenceId, parameterId, orderNumber);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Successfully added parameter {ParameterId} to sequence {SequenceId}", 
                parameterId, sequenceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding parameter {ParameterId} to sequence {SequenceId}", 
                parameterId, sequenceId);
            throw new StorageProviderException("AddParameterToSequence", ex);
        }
    }
    
    // Remove a parameter from a sequence
    public async Task RemoveParameterFromSequenceAsync(string sequenceId, string parameterId)
    {
        _logger.LogInformation("Removing parameter {ParameterId} from sequence {SequenceId}", 
            parameterId, sequenceId);
        
        try
        {
            await _unitOfWork.Parameters.RemoveParameterFromSequenceAsync(sequenceId, parameterId);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Successfully removed parameter {ParameterId} from sequence {SequenceId}", 
                parameterId, sequenceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing parameter {ParameterId} from sequence {SequenceId}", 
                parameterId, sequenceId);
            throw new StorageProviderException("RemoveParameterFromSequence", ex);
        }
    }
    
    // Validate parameter value against constraints
    public void ValidateParameterValue(Parameter parameter, string value)
    {
        if (parameter == null)
            throw new ArgumentNullException(nameof(parameter));
            
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
                if (!decimal.TryParse(value, out decimal numValue))
                {
                    _logger.LogWarning("Value '{Value}' is not a valid number for parameter {ParameterId}", 
                        value, parameter.Id);
                    throw new ValidationException(parameter.Id, value, "Value is not a valid number");
                }
                
                if (!string.IsNullOrEmpty(parameter.Min) && 
                    decimal.TryParse(parameter.Min, out decimal minValue) &&
                    numValue < minValue)
                {
                    _logger.LogWarning("Value {Value} is less than minimum value {MinValue} for parameter {ParameterId}", 
                        numValue, minValue, parameter.Id);
                    throw new ValidationException(parameter.Id, value, $"Value must be greater than or equal to {minValue}");
                }
                
                if (!string.IsNullOrEmpty(parameter.Max) && 
                    decimal.TryParse(parameter.Max, out decimal maxValue) &&
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
                    int.TryParse(parameter.Min, out int minLength) &&
                    value.Length < minLength)
                {
                    _logger.LogWarning("String length {Length} is less than minimum length {MinLength} for parameter {ParameterId}", 
                        value.Length, minLength, parameter.Id);
                    throw new ValidationException(parameter.Id, value, $"String length must be at least {minLength} characters");
                }
                
                if (!string.IsNullOrEmpty(parameter.Max) && 
                    int.TryParse(parameter.Max, out int maxLength) &&
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
    // This is useful for UI validation scenarios
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
