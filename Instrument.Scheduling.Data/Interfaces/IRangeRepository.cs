using Instrument.Scheduling.Data.Entities;

namespace Instrument.Scheduling.Data.Interfaces;

public interface IRangeRepository
{
    Task<IEnumerable<Range>> GetAllAsync();
    Task<Range?> GetByIdAsync(string id);
    Task<IQueryable<Range>> GetQueryableAsync();
    Task AddAsync(Range range);
    Task UpdateAsync(Range range);
    Task DeleteAsync(string id);
    
    // Get ranges with their values
    Task<Range?> GetRangeWithValuesAsync(string id);
    
    // Get all parameters associated with a range
    Task<IEnumerable<Parameter>> GetParametersForRangeAsync(string rangeId);
    
    Task SaveChangesAsync();
}
