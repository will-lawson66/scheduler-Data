using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;

namespace Instrument.Scheduling.Data.Services;

public class RangeService
{
    private readonly IUnitOfWork _unitOfWork;

    public RangeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Entities.Range?> GetRangeAsync(string id)
    {
        return await _unitOfWork.Ranges.GetByIdAsync(id);
    }

    public async Task<Entities.Range?> GetRangeWithValuesAsync(string id)
    {
        return await _unitOfWork.Ranges.GetRangeWithValuesAsync(id);
    }

    public async Task CreateRangeAsync(Entities.Range range)
    {
        await _unitOfWork.Ranges.AddAsync(range);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(Entities.Range range)
    {
        await _unitOfWork.Ranges.UpdateAsync(range);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteRangeAsync(string id)
    {
        await _unitOfWork.Ranges.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<Entities.Range>> GetAllRangesAsync()
    {
        return await _unitOfWork.Ranges.GetAllAsync();
    }
    
    public async Task<IEnumerable<Parameter>> GetParametersForRangeAsync(string rangeId)
    {
        return await _unitOfWork.Ranges.GetParametersForRangeAsync(rangeId);
    }
}
