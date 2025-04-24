using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;

namespace Instrument.Scheduling.Data.Services;

public class RangeValueService
{
    private readonly IUnitOfWork _unitOfWork;

    public RangeValueService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RangeValue?> GetRangeValueAsync(string id)
    {
        return await _unitOfWork.RangeValues.GetByIdAsync(id);
    }

    public async Task CreateRangeValueAsync(RangeValue rangeValue)
    {
        await _unitOfWork.RangeValues.AddAsync(rangeValue);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateRangeValueAsync(RangeValue rangeValue)
    {
        await _unitOfWork.RangeValues.UpdateAsync(rangeValue);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteRangeValueAsync(string id)
    {
        await _unitOfWork.RangeValues.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<RangeValue>> GetAllRangeValuesAsync()
    {
        return await _unitOfWork.RangeValues.GetAllAsync();
    }
    
    public async Task<IEnumerable<RangeValue>> GetValuesForRangeAsync(string rangeId)
    {
        return await _unitOfWork.RangeValues.GetValuesForRangeAsync(rangeId);
    }
    
    // Check if a value is valid for a range
    public async Task<bool> IsValueValidForRangeAsync(string rangeId, string value)
    {
        var rangeValues = await GetValuesForRangeAsync(rangeId);
        return rangeValues.Any(rv => rv.Value == value);
    }
}
