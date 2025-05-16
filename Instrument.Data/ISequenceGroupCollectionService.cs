using Instrument.Data.Entities;

namespace Instrument.Data;

public interface ISequenceGroupCollectionService<TEnum> where TEnum : Enum
{
    /// <summary>
    /// Creates a new sequence group collection
    /// </summary>
    /// <param name="sequenceGroupCollection">A <see cref="Entities.SequenceGroupCollection{TEnum}"/>></param>
    /// <returns>The created sequence group collection</returns>
    Task<SequenceGroupCollection<TEnum>> CreateSequenceGroupCollectionAsync(SequenceGroupCollection<TEnum> sequenceGroupCollection);

    /// <summary>
    /// Updates an existing sequence group collection
    /// </summary>
    /// <param name="collection">The collection with updated values</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task UpdateSequenceGroupCollectionAsync(SequenceGroupCollection<TEnum> collection);

    /// <summary>
    /// Deletes a sequence group collection
    /// </summary>
    /// <param name="id">The ID of the collection to delete</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task DeleteSequenceGroupCollectionAsync(int id);

    /// <summary>
    /// Gets a sequence group collection by ID
    /// </summary>
    /// <param name="id">The ID of the collection</param>
    /// <returns>The sequence group collection with the specified ID</returns>
    Task<SequenceGroupCollection<TEnum>?> GetSequenceGroupCollectionByIdAsync(int id);

    /// <summary>
    /// Gets all sequence group collections
    /// </summary>
    /// <returns>All sequence group collections</returns>
    Task<IEnumerable<SequenceGroupCollection<TEnum>>> GetAllSequenceGroupCollectionsAsync();

    /// <summary>
    /// Gets sequence group collections by category type
    /// </summary>
    /// <param name="category">The enum value representing the category type</param>
    /// <returns>Collections matching the category type</returns>
    Task<IEnumerable<SequenceGroupCollection<TEnum>>> GetSequenceGroupCollectionsByCategoryAsync(TEnum category);

    /// <summary>
    /// Adds a sequence to a collection
    /// </summary>
    /// <param name="collectionId">The ID of the collection</param>
    /// <param name="sequenceGroupId">The ID of the sequence</param>
    /// <param name="order">The order of the sequence in the collection</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> AddSequenceGroupToSequenceGroupCollectionAsync(int collectionId, int sequenceGroupId, int order = 0);

    /// <summary>
    /// Removes a sequence from a collection
    /// </summary>
    /// <param name="collectionId">The ID of the collection</param>
    /// <param name="sequenceGroupId">The ID of the sequence</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> RemoveSequenceGroupFromSequenceGroupCollectionAsync(int collectionId, int sequenceGroupId);

    /// <summary>
    /// Gets sequences in a collection in order
    /// </summary>
    /// <param name="collectionId">The ID of the collection</param>
    /// <returns>Ordered sequences in the collection</returns>
    Task<SortedList<int, SequenceGroup>> GetOrderedSequenceGroupsAsync(int collectionId);
}
