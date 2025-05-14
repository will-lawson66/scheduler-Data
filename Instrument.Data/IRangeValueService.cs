using Instrument.Data.Entities;

namespace Instrument.Data;

public interface IRangeValueService
{
    Task<RangeValue?> GetRangeValueByIdAsync(int id);
    Task CreateRangeValueAsync(RangeValue rangeValue);
    Task UpdateRangeValueAsync(RangeValue rangeValue);
    Task DeleteRangeValueAsync(int id);
    Task<IEnumerable<RangeValue>> GetAllRangeValuesAsync();

    /// <summary>
    /// Get the <see cref="RangeValue"/>s for this Range/>
    /// </summary>
    /// <param name="rangeId"></param>
    /// <returns></returns>
    Task<IEnumerable<RangeValue>> GetRangeValuesForRangeAsync(int rangeId);
}
