using Instrument.Scheduling.Data.Entities;

namespace Instrument.Scheduling.Data.Interfaces;

public interface IRangeRepository
{
    Task<IEnumerable<Entities.Range>> GetAllAsync();
    Task<Entities.Range?> GetByIdAsync(string id);
    Task<IQueryable<Entities.Range>> GetQueryableAsync();
    Task AddAsync(Entities.Range range);
    Task UpdateAsync(Entities.Range range);
    Task DeleteAsync(string id);
    
    // Get ranges with their values
    Task<Entities.Range?> GetRangeWithValuesAsync(string id);
    
    // Get all parameters associated with a range
    Task<IEnumerable<Parameter>> GetParametersForRangeAsync(string rangeId);
    
    Task SaveChangesAsync();
}
