using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;

namespace Instrument.Data;
/// <summary>
/// Repository interface for sequence groups
/// </summary>
public interface ISequenceGroupRepository : IRepository<SequenceGroup>
{
    /// <summary>
    /// Adds a Sequence to a SequenceGroup
    /// </summary>
    /// <param name="sequenceGroupId"></param>
    /// <param name="sequenceId"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    Task AddSequenceToSequenceGroupAsync(int sequenceGroupId, int sequenceId, int order = 0);

    /// <summary>
    /// Gets the ordered Sequences for a SequenceGroup
    /// </summary>
    /// <param name="sequenceGroupId"></param>
    /// <returns><see cref="SortedList{TKey,TValue}"/></returns>
    Task<SortedList<int, Sequence>> GetOrderedSequencesAsync(int sequenceGroupId);
    
    /// <summary>
    /// Gets SequenceGroups with their sequences filtered by name and/or technology
    /// </summary>
    /// <param name="name">Optional name filter - if null, all SequenceGroups are returned</param>
    /// <param name="technology">Optional technology filter</param>
    /// <returns>Collection of SequenceGroups with sequences loaded</returns>
    Task<IEnumerable<SequenceGroup>> GetSequenceGroupsWithSequencesAsync(string? name = null, Technology? technology = null);
}