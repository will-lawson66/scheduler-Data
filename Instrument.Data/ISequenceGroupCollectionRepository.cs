using Instrument.Data.Entities;

namespace Instrument.Data;

/// <summary>
/// Repository interface for <see cref="SequenceGroupCollection{TEnum}"/> /> entities
/// </summary>
/// <typeparam name="TEnum">The enum type used for categorization</typeparam>
public interface ISequenceGroupCollectionRepository<TEnum>
    : IRepository<SequenceGroupCollection<TEnum>> where TEnum : Enum
{
    /// <summary>
    /// Get a <see cref="SequenceGroupCollection{TEnum}" /> by category.
    /// </summary>
    /// <param name="category">An Enum expressing the tpe of the <see cref="SequenceGroupCollection{TEnum}"/></param>
    /// <returns></returns>
    Task<IEnumerable<SequenceGroupCollection<TEnum>>> GetSequenceGroupCollectionsByCategoryAsync(TEnum category);

    /// <summary>
    /// Add a <see cref="Entities.SequenceGroup"/> to a <see cref="SequenceGroupCollection{TEnum}"/> 
    /// </summary>
    /// <param name="collectionId"></param>
    /// <param name="sequenceGroupId"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    Task AddSequenceGroupToSequenceGroupCollectionAsync(int collectionId, int sequenceGroupId, int order);

    /// <summary>
    /// Get a <see cref="SortedList{TKey,TValue}"/> of the SequenceGroups in the collection
    /// </summary>
    /// <param name="collectionId"></param>
    /// <returns></returns>
    Task<SortedList<int, SequenceGroup>> GetOrderedSequenceGroupsAsync(int collectionId);
}