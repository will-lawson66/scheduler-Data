using Instrument.Data.Entities;

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
}