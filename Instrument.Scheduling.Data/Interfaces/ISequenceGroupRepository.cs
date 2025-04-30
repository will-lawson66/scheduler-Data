using Instrument.Scheduling.Data.Entities;

namespace Instrument.Scheduling.Data.Interfaces;
public interface ISequenceGroupRepository
{
    Task<IEnumerable<SequenceGroup>> GetAllAsync();
    Task<SequenceGroup?> GetByIdAsync(string id);
    Task<IQueryable<SequenceGroup>> GetQueryableAsync();
    Task AddAsync(SequenceGroup sequenceGroup);
    Task UpdateAsync(SequenceGroup sequenceGroup);
    Task DeleteAsync(string id);
    Task SaveChangesAsync();
    
    // Specialized methods for SequenceGroup
    Task<SequenceGroup?> GetWithSequencesAsync(string id);
    Task<SortedList<int, Sequence>> GetOrderedSequencesAsync(string sequenceGroupId);
}
