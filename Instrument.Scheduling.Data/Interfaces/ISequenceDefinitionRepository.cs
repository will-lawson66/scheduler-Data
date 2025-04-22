using Instrument.Scheduling.Data.Entities;

namespace Instrument.Scheduling.Data.Interfaces
{
    public interface ISequenceDefinitionRepository
    {
        Task<IEnumerable<SequenceDefinition>> GetAllAsync();
        Task<SequenceDefinition?> GetByIdAsync(string id);
        Task<IQueryable<SequenceDefinition>> GetQueryableAsync();
        Task AddAsync(SequenceDefinition sequence);
        Task UpdateAsync(SequenceDefinition sequence);
        Task DeleteAsync(string id);
        Task SaveChangesAsync();
    }
}
