using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;

namespace Instrument.Scheduling.Data.Repository;

public class ParameterRepository : IParameterRepository
{
    private readonly IStorageProvider<Parameter> _parameterStorageProvider;
    private readonly IStorageProvider<SequenceParameter> _sequenceParameterStorageProvider;
    
    public ParameterRepository(
        IStorageProvider<Parameter> parameterStorageProvider,
        IStorageProvider<SequenceParameter> sequenceParameterStorageProvider)
    {
        _parameterStorageProvider = parameterStorageProvider;
        _sequenceParameterStorageProvider = sequenceParameterStorageProvider;
    }

    public async Task<IEnumerable<Parameter>> GetAllAsync()
    {
        return await _parameterStorageProvider.GetAllAsync();
    }

    public async Task<Parameter?> GetByIdAsync(string id)
    {
        return await _parameterStorageProvider.GetByIdAsync(id);
    }

    public async Task<IQueryable<Parameter>> GetQueryableAsync()
    {
        var data = await _parameterStorageProvider.GetAllAsync();
        return data.AsQueryable();
    }

    public async Task AddAsync(Parameter parameter)
    {
        await _parameterStorageProvider.AddAsync(parameter);
    }

    public async Task UpdateAsync(Parameter parameter)
    {
        await _parameterStorageProvider.UpdateAsync(parameter);
    }

    public async Task DeleteAsync(string id)
    {
        // First, remove all SequenceParameter associations
        var sequenceParameters = await _sequenceParameterStorageProvider.GetAllAsync();
        var toDelete = sequenceParameters.Where(sp => sp.ParameterId == id).ToList();
        
        foreach (var sp in toDelete)
        {
            await _sequenceParameterStorageProvider.DeleteAsync($"{sp.SequenceId}_{sp.ParameterId}");
        }
        
        // Then delete the parameter
        await _parameterStorageProvider.DeleteAsync(id);
    }
    
    public async Task<IEnumerable<Parameter>> GetParametersForSequenceAsync(string sequenceId)
    {
        var allParameters = await _parameterStorageProvider.GetAllAsync();
        var allSequenceParams = await _sequenceParameterStorageProvider.GetAllAsync();
        
        // Find all parameter IDs associated with this sequence
        var parameterIds = allSequenceParams
            .Where(sp => sp.SequenceId == sequenceId)
            .Select(sp => sp.ParameterId);
                
        // Get the actual parameters
        return allParameters.Where(p => parameterIds.Contains(p.Id));
    }
    
    public async Task AddParameterToSequenceAsync(string sequenceId, string parameterId, int orderNumber)
    {
        var sequenceParameter = new SequenceParameter
        {
            SequenceId = sequenceId,
            ParameterId = parameterId,
            OrderNumber = orderNumber
        };
        
        await _sequenceParameterStorageProvider.AddAsync(sequenceParameter);
    }
    
    public async Task RemoveParameterFromSequenceAsync(string sequenceId, string parameterId)
    {
        // For composite key, we need to combine the keys or use a custom method
        await _sequenceParameterStorageProvider.DeleteAsync($"{sequenceId}_{parameterId}");
    }
    
    public async Task<IEnumerable<Parameter>> GetParametersByRangeAsync(string rangeId)
    {
        var parameters = await _parameterStorageProvider.GetAllAsync();
        return parameters.Where(p => p.RangeId == rangeId);
    }
    
    public async Task<IEnumerable<Parameter>> GetParametersByResourceAsync(string resourceId)
    {
        var parameters = await _parameterStorageProvider.GetAllAsync();
        return parameters.Where(p => p.ResourceId == resourceId);
    }

    public async Task SaveChangesAsync()
    {
        await _parameterStorageProvider.SaveChangesAsync();
        await _sequenceParameterStorageProvider.SaveChangesAsync();
    }
}
