using Instrument\.Data.Entities;

namespace Instrument\.Data.Interfaces;

/// <summary>
/// Repository interface for ranges
/// </summary>
public interface IRangeRepository : IRepository<Entities.Range>
{
    /// <summary>
    /// Gets a range with its values
    /// </summary>
    /// <param name="id">Range ID</param>
    Task<Entities.Range?> GetRangeWithValuesAsync(string id);
    
    /// <summary>
    /// Gets ranges by parameter
    /// </summary>
    /// <param name="parameterId">Parameter ID</param>
    Task<IEnumerable<Entities.Range>> GetRangesByParameterAsync(string parameterId);
    
    /// <summary>
    /// Adds a range value to a range
    /// </summary>
    /// <param name="rangeId">Range ID</param>
    /// <param name="name">Value name</param>
    /// <param name="value">Value</param>
    Task<RangeValue> AddRangeValueAsync(string rangeId, string name, string value);
    
    /// <summary>
    /// Removes a range value from a range
    /// </summary>
    /// <param name="rangeValueId">Range value ID</param>
    Task RemoveRangeValueAsync(string rangeValueId);
    
    /// <summary>
    /// Gets parameters for a range
    /// </summary>
    /// <param name="rangeId">Range ID</param>
    Task<IEnumerable<Parameter>> GetParametersForRangeAsync(string rangeId);
}