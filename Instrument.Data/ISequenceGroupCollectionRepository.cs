namespace Instrument.Data;

/// <summary>
/// Repository interface for <see cref="Entities.SequenceGroupCollection{TEnum}"/> /> entities
/// </summary>
/// <typeparam name="TEnum">The enum type used for categorization</typeparam>
public interface ISequenceGroupCollectionRepository<TEnum>
    : IRepository<Entities.SequenceGroupCollection<TEnum>> where TEnum : Enum
{
    /// <summary>
    /// Get a <see cref="Entities.SequenceGroupCollection{TEnum}" /> by category.
    /// </summary>
    /// <param name="category">An Enum expressing the tpe of the <see cref="Entities.SequenceGroupCollection{TEnum}"/></param>
    /// <returns></returns>
    Task<IEnumerable<Instrument.Data.Entities.SequenceGroupCollection<TEnum>>> GetSequenceGroupCollectionsByCategoryAsync(TEnum category);

    /// <summary>
    /// Add a <see cref="Entities.SequenceGroup"/> to a <see cref="Entities.SequenceGroupCollection{TEnum}"/> 
    /// </summary>
    /// <param name="collectionId"></param>
    /// <param name="sequenceGroupId"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    Task AddSequenceGroupToSequenceGroupCollectionAsync(string collectionId, string sequenceGroupId, int order);

    /// <summary>
    /// Remove a <see cref="SequenceGroup"/> from a <see cref="SequenceGroupCollection{TEnum}"/>
    /// </summary>
    /// <param name="collectionId"></param>
    /// <param name="sequenceGroupId"></param>
    /// <returns></returns>
    /// Task<bool> RemoveSequenceGroupFromSequenceGroupCollectionAsync(string collectionId, string sequenceGroupId);

    /// <summary>
    /// Get a <see cref="SortedList{TKey,TValue}"/> of the SequenceGroups in the collection
    /// </summary>
    /// <param name="collectionId"></param>
    /// <returns></returns>
    Task<SortedList<int, Instrument.Data.Entities.SequenceGroup>> GetOrderedSequenceGroupsAsync(string collectionId);
}
