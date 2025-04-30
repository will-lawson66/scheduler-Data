using Instrument.Scheduling.Data.Entities;

namespace Instrument.Scheduling.Data.Interfaces;
/// <summary>
/// Repository interface for sequence groups
/// </summary>
public interface ISequenceGroupRepository : IRepository<SequenceGroup>
{
    
    /// <summary>
    /// Gets a sequence group with its sequences
    /// </summary>
    /// <param name="id">Sequence group ID</param>
    Task<SequenceGroup?> GetWithSequencesAsync(string id);
    
    /// <summary>
    /// Gets ordered sequences for a sequence group
    /// </summary>
    /// <param name="sequenceGroupId">Sequence group ID</param>
    Task<SortedList<int, Sequence>> GetOrderedSequencesAsync(string sequenceGroupId);
}
