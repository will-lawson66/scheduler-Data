using Instrument.Data.Entities;
using Instrument.Data.Exceptions;
using Microsoft.Extensions.Logging;

namespace Instrument.Data.Services;
public class SequenceGroupCollectionService<TEnum>
    : ISequenceGroupCollectionService<TEnum>
    where TEnum : Enum
{
    private readonly ISequenceGroupCollectionRepository<TEnum> _repository;
    private readonly ISequenceGroupRepository _sequenceGroupRepository;
    private readonly ILogger<SequenceGroupCollectionService<TEnum>> _logger;

    public SequenceGroupCollectionService(ISequenceGroupCollectionRepository<TEnum> repository,
        Instrument.Data.ISequenceGroupRepository sequenceGroupRepository,
        ILogger<SequenceGroupCollectionService<TEnum>> logger)
    {
        _repository = repository;
        _sequenceGroupRepository = sequenceGroupRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SequenceGroupCollection<TEnum>> CreateSequenceGroupCollectionAsync(
        SequenceGroupCollection<TEnum> sequenceGroupCollection)
    {
        _logger.LogInformation("Creating SequenceGroupCollection with Name: {Name}", sequenceGroupCollection.Name);

        // Validate inputs
        if (string.IsNullOrWhiteSpace(sequenceGroupCollection.Name))
        {
            throw new ArgumentException("SequenceGroupCollection name cannot be empty", nameof(sequenceGroupCollection));
        }

        try
        {
            await _repository.AddAsync(sequenceGroupCollection);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("SequenceGroupCollection created successfully: {Name}", sequenceGroupCollection.Name);
            return sequenceGroupCollection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating SequenceGroupCollection with Id: {Name}", sequenceGroupCollection.Name);
            throw new StorageProviderException("CreateSequenceGroup", ex);
        }
    }

    /// <inheritdoc />
    public async Task UpdateSequenceGroupCollectionAsync(SequenceGroupCollection<TEnum> collection)
    {
        if (collection is null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        _logger.LogInformation("Updating SequenceGroupCollection with Id: {Id}", collection.Id);

        try
        {
            // Validate if the sequence exists
            var existingSequenceGroupCollection = await _repository.GetByIdAsync(collection.Id);
            if (existingSequenceGroupCollection == null)
            {
                throw new EntityNotFoundException("SequenceGroupCollection", collection.Id);
            }

            await _repository.UpdateAsync(collection);
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Successfully updated SequenceGroupCollection with Id: {Id}", collection.Id);
        }
        catch (EntityNotFoundException)
        {
            _logger.LogWarning("SequenceGroupCollection with Id {Id} does not exist", collection.Id);
            throw;
        }
        catch (StorageProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating SequenceGroupCollection with Id: {Id}", collection.Id);
            throw new StorageProviderException("UpdateSequenceGroupCollection", ex);
        }
    }

    /// <inheritdoc />
    public async Task DeleteSequenceGroupCollectionAsync(int sequenceGroupCollectionId)
    {
        try
        {
            // Validate if the sequence group collection exists
            var sequenceGroup = await _repository.GetByIdAsync(sequenceGroupCollectionId);
            if (sequenceGroup == null)
            {
                throw new EntityNotFoundException("SequenceGroupCollection", sequenceGroupCollectionId);
            }

            await _repository.DeleteAsync(sequenceGroupCollectionId);
            await _repository.SaveChangesAsync();
        }
        catch (EntityNotFoundException)
        {
            _logger.LogError("Sequence group collection not found: {Id}", sequenceGroupCollectionId);
            throw;
        }
        catch (StorageProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sequence group collection: {Id}", sequenceGroupCollectionId);
            throw new StorageProviderException("DeleteSequenceGroupCollection", ex);
        }
    }
    
    /// <inheritdoc />
    public async Task<SequenceGroupCollection<TEnum>?> GetSequenceGroupCollectionByIdAsync(int sequenceGroupCollectionId)
    {
        try
        {
            return await _repository.GetByIdAsync(sequenceGroupCollectionId);
        }
        catch (StorageProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sequence group collection by ID: {Id}", sequenceGroupCollectionId);
            throw new StorageProviderException("GetSequenceGroupCollectionById", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SequenceGroupCollection<TEnum>>> GetAllSequenceGroupCollectionsAsync()
    {
        try
        {
            return await _repository.GetAllAsync();
        }
        catch (StorageProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all sequence groups");
            throw new StorageProviderException("GetAllSequenceGroupCollections", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SequenceGroupCollection<TEnum>>> GetSequenceGroupCollectionsByCategoryAsync(TEnum category)
    {
        try
        {
            return await _repository.GetSequenceGroupCollectionsByCategoryAsync(category);
        }
        catch (StorageProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sequence group collections by category {CategoryType}", category);
            throw new StorageProviderException($"GetSequenceGroupCollectionsByCategory", ex);
        }
    }
    /// <inheritdoc />
    public async Task<bool> AddSequenceGroupToSequenceGroupCollectionAsync(int sequenceGroupCollectionId, int sequenceGroupId, int order = 0)
    {
        _logger.LogInformation("Adding sequence group {groupId} to sequence group collection {collectionId} at order {order}",
            sequenceGroupCollectionId, sequenceGroupId, order);

        try
        {
            // Get the sequence group collection
            var sequenceGroupCollection = await _repository.GetByIdAsync(sequenceGroupCollectionId);
            if (sequenceGroupCollection == null)
            {
                _logger.LogWarning("Sequence group collection not found: {Id}", sequenceGroupCollectionId);
                return false;
            }

            // Get the sequence group
            var sequenceGroup = await _sequenceGroupRepository.GetByIdAsync(sequenceGroupId);
            if (sequenceGroup == null)
            {
                _logger.LogWarning("Sequence group not found: {Id}", sequenceGroupId);
                return false;
            }

            // Add the sequence group to the collection
            await _repository.AddSequenceGroupToSequenceGroupCollectionAsync(sequenceGroupCollectionId, sequenceGroupId, order);
            _logger.LogInformation("Successfully added sequence group {SequenceGroup} to collection {SequenceGroupCollection} at order {Order}",
                sequenceGroupId, sequenceGroupCollectionId, order);

            return true;
        }
        catch (EntityNotFoundException notFound)
        {
            _logger.LogError("{Entity} not found: {Id}", notFound.EntityType, notFound.EntityId);
            throw;
        }
        catch (StorageProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding sequence group {GroupId} to sequence group collection {CollectionId}",
                sequenceGroupId, sequenceGroupCollectionId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RemoveSequenceGroupFromSequenceGroupCollectionAsync(int sequenceGroupCollectionId, int sequenceGroupId)
    {
        try
        {
            // Validate if the sequence group collection exists
            var sequenceGroupCollection = await _repository.GetByIdAsync(sequenceGroupCollectionId);
            if (sequenceGroupCollection == null)
            {
                throw new EntityNotFoundException("SequenceGroupCollection", sequenceGroupCollectionId);
            }

            // Find the sequence-group association
            var sequenceGroupCollectionSequenceGroup = sequenceGroupCollection.SequenceGroupCollectionSequenceGroups
                .FirstOrDefault(collection =>
                    collection.SequenceGroupCollectionId == sequenceGroupCollectionId &&
                    collection.SequenceGroupId == sequenceGroupId);

            if (sequenceGroupCollectionSequenceGroup == null)
            {
                _logger.LogWarning("Sequence group {SequenceGroupId} not found in sequence group collection {CollectionId}",
                    sequenceGroupId, sequenceGroupCollectionId);
                return false;
            }

            // Remove the association
            // todo: does this work as expected? test 
            sequenceGroupCollection.SequenceGroupCollectionSequenceGroups.Remove(sequenceGroupCollectionSequenceGroup);
            await _sequenceGroupRepository.SaveChangesAsync();

            _logger.LogInformation("Removed sequence group {SequenceGroupId} from sequence group collection {CollectionId}",
                sequenceGroupId, sequenceGroupCollectionId);

            return true;
        }
        catch (EntityNotFoundException)
        {
            _logger.LogError("Sequence group collection not found: {Id}", sequenceGroupCollectionId);
            throw;
        }
        catch (StorageProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing sequence group {SequenceGroupId} from sequence group collection {CollectionId}",
                sequenceGroupId, sequenceGroupCollectionId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<SortedList<int, SequenceGroup>> GetOrderedSequenceGroupsAsync(int sequenceGroupCollectionId)
    {
        _logger.LogInformation("Getting ordered sequence groups for sequence group collection: {Id}", sequenceGroupCollectionId);
        try
        {
            // Validate if the sequence group exists
            var sequenceGroup = await _repository.GetByIdAsync(sequenceGroupCollectionId);
            if (sequenceGroup == null)
            {
                throw new EntityNotFoundException("SequenceGroupCollection", sequenceGroupCollectionId);
            }

            return await _repository.GetOrderedSequenceGroupsAsync(sequenceGroupCollectionId);
        }
        catch (EntityNotFoundException)
        {
            _logger.LogError("Sequence group collection not found: {Id}", sequenceGroupCollectionId);
            throw;
        }
        catch (StorageProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ordered sequence groups for sequence group collection: {Id}", sequenceGroupCollectionId);
            throw new StorageProviderException("GetOrderedSequencesGroups", ex);
        }
    }
}
