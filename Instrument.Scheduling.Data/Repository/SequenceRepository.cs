using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;

namespace Instrument.Scheduling.Data.Repository;
public class SequenceRepository : ISequenceRepository
{
    private readonly IStorageProvider<Sequence> _storageProvider;

    public SequenceRepository(IStorageProvider<Sequence> storageProvider)
    {
        _storageProvider = storageProvider;
    }

    public async Task<IEnumerable<Sequence>> GetAllAsync()
    {
        return await _storageProvider.GetAllAsync();
    }

    public async Task<Sequence?> GetByIdAsync(string id)
    {
        return await _storageProvider.GetByIdAsync(id);
    }

    public async Task<IQueryable<Sequence>> GetQueryableAsync()
    {
        var data = await _storageProvider.GetAllAsync();
        return data.AsQueryable();
    }

    public async Task AddAsync(Sequence sequence)
    {
        await _storageProvider.AddAsync(sequence);
    }

    public async Task UpdateAsync(Sequence sequence)
    {
        //sequence.ModifiedDate = DateTime.UtcNow;
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
