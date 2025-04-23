using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;

namespace Instrument.Scheduling.Data.Services;

public class ParameterService
{
    private readonly IUnitOfWork _unitOfWork;

    public ParameterService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Parameter?> GetParameterAsync(string id)
    {
        return await _unitOfWork.Parameters.GetByIdAsync(id);
    }

    public async Task CreateParameterAsync(Parameter parameter)
    {
        await _unitOfWork.Parameters.AddAsync(parameter);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateParameterAsync(Parameter parameter)
    {
        await _unitOfWork.Parameters.UpdateAsync(parameter);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteParameterAsync(string id)
    {
        await _unitOfWork.Parameters.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<Parameter>> GetAllParametersAsync()
    {
        return await _unitOfWork.Parameters.GetAllAsync();
    }
    
    // Get parameters for a specific sequence
    public async Task<IEnumerable<Parameter>> GetParametersForSequenceAsync(string sequenceId)
    {
        return await _unitOfWork.Parameters.GetParametersForSequenceAsync(sequenceId);
    }
    
    // Add a parameter to a sequence
    public async Task AddParameterToSequenceAsync(string sequenceId, string parameterId, string? overrideValue = null)
    {
        await _unitOfWork.Parameters.AddParameterToSequenceAsync(sequenceId, parameterId, overrideValue);
        await _unitOfWork.SaveChangesAsync();
    }
    
    // Remove a parameter from a sequence
    public async Task RemoveParameterFromSequenceAsync(string sequenceId, string parameterId)
    {
        await _unitOfWork.Parameters.RemoveParameterFromSequenceAsync(sequenceId, parameterId);
        await _unitOfWork.SaveChangesAsync();
    }
    
    // Update a parameter's override value in a sequence
    public async Task UpdateParameterOverrideValueAsync(string sequenceId, string parameterId, string? overrideValue)
    {
        await _unitOfWork.Parameters.UpdateParameterOverrideValueAsync(sequenceId, parameterId, overrideValue);
        await _unitOfWork.SaveChangesAsync();
    }
    
    // Validate parameter value against constraints
    public bool ValidateParameterValue(Parameter parameter, string value)
    {
        if (parameter.Required && string.IsNullOrEmpty(value))
        {
            return false;
        }
        
        if (string.IsNullOrEmpty(value))
        {
            // Not required and empty is valid
            return true;
        }
        
        switch (parameter.ParameterType.ToLower())
        {
            case "number":
            case "integer":
            case "float":
            case "double":
                if (!double.TryParse(value, out double numValue))
                {
                    return false;
                }
                
                if (!string.IsNullOrEmpty(parameter.MinValue) && 
                    double.TryParse(parameter.MinValue, out double minValue) &&
                    numValue < minValue)
                {
                    return false;
                }
                
                if (!string.IsNullOrEmpty(parameter.MaxValue) && 
                    double.TryParse(parameter.MaxValue, out double maxValue) &&
                    numValue > maxValue)
                {
                    return false;
                }
                
                return true;
                
            case "string":
                if (!string.IsNullOrEmpty(parameter.MinValue) && 
                    int.TryParse(parameter.MinValue, out int minLength) &&
                    value.Length < minLength)
                {
                    return false;
                }
                
                if (!string.IsNullOrEmpty(parameter.MaxValue) && 
                    int.TryParse(parameter.MaxValue, out int maxLength) &&
                    value.Length > maxLength)
                {
                    return false;
                }
                
                return true;
                
            case "boolean":
                return bool.TryParse(value, out _);
                
            default:
                // For custom types, just return true
                return true;
        }
    }
}
