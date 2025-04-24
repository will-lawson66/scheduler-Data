using Instrument.Scheduling.Data.Entities;

namespace Instrument.Scheduling.Data.Interfaces;

public interface IRangeValueRepository
{
    Task<IEnumerable<RangeValue>> GetAllAsync();
    Task<RangeValue?> GetByIdAsync(string id);
    Task<IQueryable<RangeValue>> GetQueryableAsync();
    Task AddAsync(RangeValue rangeValue);
    Task UpdateAsync(RangeValue rangeValue);
    Task DeleteAsync(string id);
    
    // Get all range values for a specific range
    Task<IEnumerable<RangeValue>> GetValuesForRangeAsync(string rangeId);
    
    Task SaveChangesAsync();
}
