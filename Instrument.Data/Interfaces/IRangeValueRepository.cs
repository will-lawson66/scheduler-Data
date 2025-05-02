using Instrument.Data.Entities;

namespace Instrument.Data.Interfaces;

/// <summary>
/// Repository interface for range values
/// </summary>
public interface IRangeValueRepository : IRepository<RangeValue>
{
    /// <summary>
    /// Gets range values by range ID
    /// </summary>
    /// <param name="rangeId">Range ID</param>
    Task<IEnumerable<RangeValue>> GetValuesByRangeIdAsync(string rangeId);
    
    /// <summary>
    /// Gets a range value by name and range ID
    /// </summary>
    /// <param name="name">Value name</param>
    /// <param name="rangeId">Range ID</param>
    Task<RangeValue?> GetByNameAndRangeIdAsync(string name, string rangeId);
    
    /// <summary>
    /// Updates a range value
    /// </summary>
    /// <param name="id">Value ID</param>
    /// <param name="name">New name</param>
    /// <param name="value">New value</param>
    Task UpdateRangeValueAsync(string id, string name, string value);
    
    /// <summary>
    /// Gets values for a range (alternative naming for GetValuesByRangeIdAsync)
    /// </summary>
    /// <param name="rangeId">Range ID</param>
    Task<IEnumerable<RangeValue>> GetValuesForRangeAsync(string rangeId);
}