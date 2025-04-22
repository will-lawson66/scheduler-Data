using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;

namespace Instrument.Scheduling.Data.Repository;
public class SequenceDefinitionRepository : ISequenceDefinitionRepository
{
    private readonly IStorageProvider<SequenceDefinition> _storageProvider;

    public SequenceDefinitionRepository(IStorageProvider<SequenceDefinition> storageProvider)
    {
        _storageProvider = storageProvider;
    }

    public async Task<IEnumerable<SequenceDefinition>> GetAllAsync()
    {
        return await _storageProvider.GetAllAsync();
    }

    public async Task<SequenceDefinition?> GetByIdAsync(string id)
    {
        return await _storageProvider.GetByIdAsync(id);
    }

    public async Task<IQueryable<SequenceDefinition>> GetQueryableAsync()
    {
        var data = await _storageProvider.GetAllAsync();
        return data.AsQueryable();
    }

    public async Task AddAsync(SequenceDefinition sequence)
    {
        await _storageProvider.AddAsync(sequence);
    }

    public async Task UpdateAsync(SequenceDefinition sequence)
    {
        sequence.ModifiedDate = DateTime.UtcNow;
        await _storageProvider.UpdateAsync(sequence);
    }

    public async Task DeleteAsync(string id)
    {
        await _storageProvider.DeleteAsync(id);
    }

    public async Task SaveChangesAsync()
    {
        await _storageProvider.SaveChangesAsync();
    }
}
