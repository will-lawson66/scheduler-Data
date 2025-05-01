using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;

namespace Instrument.Scheduling.Data.Services;

public class RangeService
{
    private readonly IRangeRepository _rangeRepository;

    public RangeService(IRangeRepository rangeRepository)
    {
        _rangeRepository = rangeRepository;
    }

    public async Task<Entities.Range?> GetRangeAsync(string id)
    {
        return await _rangeRepository.GetByIdAsync(id);
    }

    public async Task<Entities.Range?> GetRangeWithValuesAsync(string id)
    {
        return await _rangeRepository.GetRangeWithValuesAsync(id);
    }

    public async Task CreateRangeAsync(Entities.Range range)
    {
        await _rangeRepository.AddAsync(range);
        await _rangeRepository.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(Entities.Range range)
    {
        await _rangeRepository.UpdateAsync(range);
        await _rangeRepository.SaveChangesAsync();
    }

    public async Task DeleteRangeAsync(string id)
    {
        await _rangeRepository.DeleteAsync(id);
        await _rangeRepository.SaveChangesAsync();
    }

    public async Task<IEnumerable<Entities.Range>> GetAllRangesAsync()
    {
        return await _rangeRepository.GetAllAsync();
    }
    
    public async Task<IEnumerable<Parameter>> GetParametersForRangeAsync(string rangeId)
    {
        return await _rangeRepository.GetParametersForRangeAsync(rangeId);
    }
}
