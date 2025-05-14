namespace Instrument.Data;

/// <summary>
/// Repository interface for ranges
/// </summary>
public interface IRangeRepository : IRepository<Entities.Range>
{
    /// <summary>
    /// Gets a range with its values
    /// </summary>
    /// <param name="rangeId">Range ID</param>
    Task<Entities.Range?> GetRangeWithRangeValuesByIdAsync(int rangeId);
}