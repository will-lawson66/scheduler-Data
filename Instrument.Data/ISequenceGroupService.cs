using Instrument.Data.Entities;

namespace Instrument.Data;

public interface ISequenceGroupService
{
    Task<SequenceGroup> CreateSequenceGroupAsync(SequenceGroup sequenceGroup);
    Task UpdateSequenceGroupAsync(SequenceGroup sequenceGroup);
    Task<SequenceGroup?> GetSequenceGroupByIdAsync(int sequenceGroupId);
    Task<IEnumerable<SequenceGroup>> GetAllSequenceGroupsAsync();
    Task DeleteSequenceGroupAsync(int sequenceGroupId);

    /// <summary>
    /// Adds a sequence to a sequence group in a specific order
    /// </summary>
    /// <param name="sequenceGroupId">Identifier of the sequence group</param>
    /// <param name="sequenceId">Identifier of the sequence to add</param>
    /// <param name="order">Order/position of the sequence within the group</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> AddSequenceToSequenceGroupAsync(int sequenceGroupId, int sequenceId, int order = 0);

    /// <summary>
    /// Gets all sequences in a sequence group in their specified order
    /// </summary>
    /// <param name="sequenceGroupId">Identifier of the sequence group</param>
    /// <returns><see cref="SortedList{TKey,TValue}"/>A sorted list of sequences sorted by order.</returns>
    Task<SortedList<int, Sequence>> GetOrderedSequencesAsync(int sequenceGroupId);

    /// <summary>
    /// Removes a sequence from a sequence group
    /// </summary>
    /// <param name="sequenceGroupId">Identifier of the sequence group</param>
    /// <param name="sequenceId">Identifier of the sequence to remove</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> RemoveSequenceFromSequenceGroupAsync(int sequenceGroupId, int sequenceId);

    /// <summary>
    /// Validates a sequence group according to business rules
    /// </summary>
    /// <param name="sequenceGroupId">Identifier of the sequence group to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    Task<bool> ValidateSequenceGroupAsync(int sequenceGroupId);
}
