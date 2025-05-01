using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;

namespace Instrument.Scheduling.Data.Services;

public class RangeValueService
{
    private readonly IRangeValueRepository _rangeValueRepository;

    public RangeValueService(IRangeValueRepository rangeValueRepository)
    {
        _rangeValueRepository = rangeValueRepository;
    }

    public async Task<RangeValue?> GetRangeValueAsync(string id)
    {
        return await _rangeValueRepository.GetByIdAsync(id);
    }

    public async Task CreateRangeValueAsync(RangeValue rangeValue)
    {
        await _rangeValueRepository.AddAsync(rangeValue);
        await _rangeValueRepository.SaveChangesAsync();
    }

    public async Task UpdateRangeValueAsync(RangeValue rangeValue)
    {
        await _rangeValueRepository.UpdateAsync(rangeValue);
        await _rangeValueRepository.SaveChangesAsync();
    }

    public async Task DeleteRangeValueAsync(string id)
    {
        await _rangeValueRepository.DeleteAsync(id);
        await _rangeValueRepository.SaveChangesAsync();
    }

    public async Task<IEnumerable<RangeValue>> GetAllRangeValuesAsync()
    {
        return await _rangeValueRepository.GetAllAsync();
    }
    
    public async Task<IEnumerable<RangeValue>> GetValuesForRangeAsync(string rangeId)
    {
        return await _rangeValueRepository.GetValuesForRangeAsync(rangeId);
    }
    
    // Check if a value is valid for a range
    public async Task<bool> IsValueValidForRangeAsync(string rangeId, string value)
    {
        var rangeValues = await GetValuesForRangeAsync(rangeId);
        return rangeValues.Any(rv => rv.Value == value);
    }
}
