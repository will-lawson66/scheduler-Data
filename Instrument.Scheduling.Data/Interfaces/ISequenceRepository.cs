using Instrument.Scheduling.Data.Entities;

namespace Instrument.Scheduling.Data.Interfaces;
public interface ISequenceRepository
{
    Task<IEnumerable<Sequence>> GetAllAsync();
    Task<Sequence?> GetByIdAsync(string id);
    Task<IQueryable<Sequence>> GetQueryableAsync();
    Task AddAsync(Sequence sequence);
    Task UpdateAsync(Sequence sequence);
    Task DeleteAsync(string id);
    Task SaveChangesAsync();
}
