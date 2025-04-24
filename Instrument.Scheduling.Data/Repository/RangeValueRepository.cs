using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;

namespace Instrument.Scheduling.Data.Repository;

public class RangeValueRepository : IRangeValueRepository
{
    private readonly IStorageProvider<RangeValue> _rangeValueStorageProvider;

    public RangeValueRepository(IStorageProvider<RangeValue> rangeValueStorageProvider)
    {
        _rangeValueStorageProvider = rangeValueStorageProvider;
    }

    public async Task<IEnumerable<RangeValue>> GetAllAsync()
    {
        return await _rangeValueStorageProvider.GetAllAsync();
    }

    public async Task<RangeValue?> GetByIdAsync(string id)
    {
        return await _rangeValueStorageProvider.GetByIdAsync(id);
    }

    public async Task<IQueryable<RangeValue>> GetQueryableAsync()
    {
        var data = await _rangeValueStorageProvider.GetAllAsync();
        return data.AsQueryable();
    }

    public async Task AddAsync(RangeValue rangeValue)
    {
        await _rangeValueStorageProvider.AddAsync(rangeValue);
    }

    public async Task UpdateAsync(RangeValue rangeValue)
    {
        await _rangeValueStorageProvider.UpdateAsync(rangeValue);
    }

    public async Task DeleteAsync(string id)
    {
        await _rangeValueStorageProvider.DeleteAsync(id);
    }
    
    public async Task<IEnumerable<RangeValue>> GetValuesForRangeAsync(string rangeId)
    {
        var values = await _rangeValueStorageProvider.GetAllAsync();
        return values.Where(v => v.RangeId == rangeId);
    }

    public async Task SaveChangesAsync()
    {
        await _rangeValueStorageProvider.SaveChangesAsync();
    }
}
