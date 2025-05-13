namespace Instrument.Data;

/// <summary>
/// Repository interface for range values
/// </summary>
public interface IRangeValueRepository : IRepository<Instrument.Data.Entities.RangeValue>
{
    /// <summary>
    /// Gets range values by range ID
    /// </summary>
    /// <param name="rangeId">Range ID</param>
    Task<IEnumerable<Instrument.Data.Entities.RangeValue>> GetRangeValuesByRangeIdAsync(string rangeId);
}