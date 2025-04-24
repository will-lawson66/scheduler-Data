using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Scheduling.Data.Repository;

public class RangeRepository : IRangeRepository
{
    private readonly IStorageProvider<Range> _rangeStorageProvider;
    private readonly IStorageProvider<Parameter> _parameterStorageProvider;

    public RangeRepository(
        IStorageProvider<Range> rangeStorageProvider,
        IStorageProvider<Parameter> parameterStorageProvider)
    {
        _rangeStorageProvider = rangeStorageProvider;
        _parameterStorageProvider = parameterStorageProvider;
    }

    public async Task<IEnumerable<Range>> GetAllAsync()
    {
        return await _rangeStorageProvider.GetAllAsync();
    }

    public async Task<Range?> GetByIdAsync(string id)
    {
        return await _rangeStorageProvider.GetByIdAsync(id);
    }

    public async Task<IQueryable<Range>> GetQueryableAsync()
    {
        var data = await _rangeStorageProvider.GetAllAsync();
        return data.AsQueryable();
    }

    public async Task AddAsync(Range range)
    {
        await _rangeStorageProvider.AddAsync(range);
    }

    public async Task UpdateAsync(Range range)
    {
        await _rangeStorageProvider.UpdateAsync(range);
    }

    public async Task DeleteAsync(string id)
    {
        await _rangeStorageProvider.DeleteAsync(id);
    }
    
    public async Task<Range?> GetRangeWithValuesAsync(string id)
    {
        // This works when using EF Core, but for our simplified storage provider
        // we would need to separately retrieve the values
        var range = await _rangeStorageProvider.GetByIdAsync(id);
        if (range == null)
            return null;
            
        // This is a simplified approach - in a real implementation with
        // EF Core we would use Include() to load related entities
        return range;
    }
    
    public async Task<IEnumerable<Parameter>> GetParametersForRangeAsync(string rangeId)
    {
        var parameters = await _parameterStorageProvider.GetAllAsync();
        return parameters.Where(p => p.RangeId == rangeId);
    }

    public async Task SaveChangesAsync()
    {
        await _rangeStorageProvider.SaveChangesAsync();
    }
}
