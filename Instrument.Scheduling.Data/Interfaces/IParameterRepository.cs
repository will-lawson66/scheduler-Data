using Instrument.Scheduling.Data.Entities;

namespace Instrument.Scheduling.Data.Interfaces;

public interface IParameterRepository
{
    Task<IEnumerable<Parameter>> GetAllAsync();
    Task<Parameter?> GetByIdAsync(string id);
    Task<IQueryable<Parameter>> GetQueryableAsync();
    Task AddAsync(Parameter parameter);
    Task UpdateAsync(Parameter parameter);
    Task DeleteAsync(string id);
    
    // Get parameters for a specific sequence
    Task<IEnumerable<Parameter>> GetParametersForSequenceAsync(string sequenceId);
    
    // Add a parameter to a sequence
    Task AddParameterToSequenceAsync(string sequenceId, string parameterId, string? overrideValue = null);
    
    // Remove a parameter from a sequence
    Task RemoveParameterFromSequenceAsync(string sequenceId, string parameterId);
    
    // Update a parameter's override value in a sequence
    Task UpdateParameterOverrideValueAsync(string sequenceId, string parameterId, string? overrideValue);
    
    Task SaveChangesAsync();
}
