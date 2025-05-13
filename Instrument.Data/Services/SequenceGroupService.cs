using Instrument.Data.Entities;
using Instrument.Data.Exceptions;
using Microsoft.Extensions.Logging;

namespace Instrument.Data.Services;

/// <summary>
/// Service for managing sequence groups
/// </summary>
public class SequenceGroupService : ISequenceGroupService
{
    private readonly ISequenceGroupRepository _sequenceGroupRepository;
    private readonly ISequenceRepository _sequenceRepository;
    private readonly ILogger<SequenceGroupService> _logger;

    public SequenceGroupService(
        ISequenceGroupRepository sequenceGroupRepository,
        ISequenceRepository sequenceRepository,
        ILogger<SequenceGroupService> logger)
    {
        _sequenceGroupRepository = sequenceGroupRepository;
        _sequenceRepository = sequenceRepository;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new sequence group
    /// </summary>
    /// <param name="id">Identifier for the sequence group</param>
    /// <param name="name">Name of the sequence group</param>
    /// <param name="description">Optional description of the sequence group</param>
    /// <returns>The created sequence group</returns>
    public async Task<SequenceGroup> CreateSequenceGroupAsync(string id, string name, string? description = null)
    {
        _logger.LogInformation("Creating sequence group with ID: {Id}", id);

        // Validate inputs
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Sequence group ID cannot be empty", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Sequence group name cannot be empty", nameof(name));
        }

        // Check if ID already exists
        var existingGroup = await _sequenceGroupRepository.GetByIdAsync(id);
        if (existingGroup != null)
        {
            throw new SchedulerDataException($"Sequence group with ID {id} already exists");
        }

        // Create and save the sequence group
        var sequenceGroup = new SequenceGroup
        {
            Id = id,
            Name = name,
            Description = description
        };

        try
        {
            await _sequenceGroupRepository.AddAsync(sequenceGroup);
            await _sequenceGroupRepository.SaveChangesAsync();

            _logger.LogInformation("Sequence group created successfully: {Id}", id);
            return sequenceGroup;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sequence group with ID: {Id}", id);
            throw new StorageProviderException("CreateSequenceGroup", ex);
        }
    }

    /// <summary>
    /// Update an existing SequenceGroup
    /// </summary>
    /// <param name="sequenceGroup"><see cref="SequenceGroup"/></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="EntityNotFoundException"></exception>
    /// <exception cref="StorageProviderException"></exception>
    public async Task UpdateSequenceGroupAsync(SequenceGroup sequenceGroup)
    {
        if (sequenceGroup == null)
        {
            throw new ArgumentNullException(nameof(sequenceGroup));
        }

        _logger.LogInformation("Updating SequenceGroup with ID: {Id}", sequenceGroup.Id);

        // Validate if the sequence exists
        var existingSequenceGroup = await _sequenceGroupRepository.GetByIdAsync(sequenceGroup.Id);
        if (existingSequenceGroup == null)
        {
            _logger.LogWarning("SequenceGroup with ID {Id} does not exist", sequenceGroup.Id);
            throw new EntityNotFoundException("SequenceGroup", sequenceGroup.Id);
        }

        try
        {
            await _sequenceGroupRepository.UpdateAsync(sequenceGroup);
            await _sequenceGroupRepository.SaveChangesAsync();
            _logger.LogInformation("Successfully updated SequenceGroup with ID: {Id}", sequenceGroup.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating SequenceGroup with ID: {Id}", sequenceGroup.Id);
            throw new StorageProviderException("UpdateSequenceGroup", ex);
        }
    }

    /// <summary>
    /// Gets a sequence group by its identifier
    /// </summary>
    /// <param name="sequenceGroupId">Identifier of the sequence group</param>
    /// <returns>The sequence group if found, null otherwise</returns>
    public async Task<SequenceGroup?> GetSequenceGroupByIdAsync(string sequenceGroupId)
    {
        try
        {
            return await _sequenceGroupRepository.GetByIdAsync(sequenceGroupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sequence group by ID: {Id}", sequenceGroupId);
            throw new StorageProviderException("GetSequenceGroupById", ex);
        }
    }

    /// <summary>
    /// Gets all sequence groups
    /// </summary>
    /// <returns>Collection of all sequence groups</returns>
    public async Task<IEnumerable<SequenceGroup>> GetAllSequenceGroupsAsync()
    {
        try
        {
            return await _sequenceGroupRepository.GetAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all sequence groups");
            throw new StorageProviderException("GetAllSequenceGroups", ex);
        }
    }
    
    /// <summary>
    /// Deletes a sequence group
    /// </summary>
    /// <param name="sequenceGroupId">Identifier of the sequence group to delete</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task DeleteSequenceGroupAsync(string sequenceGroupId)
    {
        try
        {
            // Validate if the sequence group exists
            var sequenceGroup = await _sequenceGroupRepository.GetByIdAsync(sequenceGroupId);
            if (sequenceGroup == null)
            {
                throw new EntityNotFoundException("SequenceGroup", sequenceGroupId);
            }

            await _sequenceGroupRepository.DeleteAsync(sequenceGroupId);
            await _sequenceGroupRepository.SaveChangesAsync();
        }
        catch (EntityNotFoundException)
        {
            _logger.LogError("Sequence group not found: {Id}", sequenceGroupId);
            // Re-throw entity not found exceptions
            throw;
        }
        catch (StorageProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sequence group {Id}", sequenceGroupId);
            throw new StorageProviderException("DeleteSequenceGroup", ex);
        }
    }

    /// <summary>
    /// Adds a sequence to a sequence group in a specific order
    /// </summary>
    /// <param name="sequenceGroupId">Identifier of the sequence group</param>
    /// <param name="sequenceId">Identifier of the sequence to add</param>
    /// <param name="order">Order/position of the sequence within the group</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> AddSequenceToSequenceGroupAsync(string sequenceGroupId, string sequenceId, int order)
    {
        _logger.LogInformation("Adding sequence {SequenceId} to group {GroupId} at order {Order}",
            sequenceId, sequenceGroupId, order);

        try
        {
            // Get the sequence group
            var sequenceGroup = await _sequenceGroupRepository.GetByIdAsync(sequenceGroupId);
            if (sequenceGroup == null)
            {
                _logger.LogError("Sequence group not found: {Id}", sequenceGroupId);
                throw new EntityNotFoundException("SequenceGroup", sequenceGroupId);
            }

            // Get the sequence
            var sequence = await _sequenceRepository.GetByIdAsync(sequenceId);
            if (sequence == null)
            {
                _logger.LogError("Sequence not found: {Id}", sequenceId);
                throw new EntityNotFoundException("Sequence", sequenceId);
            }

            // Add the sequence to the group
            await _sequenceGroupRepository.AddSequenceToSequenceGroupAsync(sequenceGroupId, sequenceId, order);
            _logger.LogInformation("Successfully added sequence {SequenceId} to group {GroupId} at order {Order}",
                sequenceId, sequenceGroupId, order);

            return true;
        }
        catch (EntityNotFoundException)
        {
            _logger.LogError("Sequence group not found: {Id}", sequenceGroupId);
            // Re-throw entity not found exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding sequence {SequenceId} to group {GroupId}",
                sequenceId, sequenceGroupId);
            return false;
        }
    }

    /// <summary>
    /// Gets all sequences in a sequence group in their specified order
    /// </summary>
    /// <param name="sequenceGroupId">Identifier of the sequence group</param>
    /// <returns><see cref="SortedList{TKey,TValue}"/>A sorted list of sequences sorted by order.</returns>
    public async Task<SortedList<int, Sequence>> GetOrderedSequencesAsync(string sequenceGroupId)
    {
        _logger.LogInformation("Getting ordered sequences for group: {Id}", sequenceGroupId);
        try
        {
            // Validate if the sequence group exists
            var sequenceGroup = await _sequenceGroupRepository.GetByIdAsync(sequenceGroupId);
            if (sequenceGroup == null)
            {
                throw new EntityNotFoundException("SequenceGroup", sequenceGroupId);
            }

            return await _sequenceGroupRepository.GetOrderedSequencesAsync(sequenceGroupId);
        }
        catch (EntityNotFoundException)
        {
            _logger.LogError("Sequence group not found: {Id}", sequenceGroupId);
            // Re-throw entity not found exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ordered sequences for group: {Id}", sequenceGroupId);
            throw new StorageProviderException("GetOrderedSequences", ex);
        }
    }

    /// <summary>
    /// Removes a sequence from a sequence group
    /// </summary>
    /// <param name="sequenceGroupId">Identifier of the sequence group</param>
    /// <param name="sequenceId">Identifier of the sequence to remove</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> RemoveSequenceFromSequenceGroupAsync(string sequenceGroupId, string sequenceId)
    {
        try
        {
            // Validate if the sequence group exists
            var sequenceGroup = await _sequenceGroupRepository.GetByIdAsync(sequenceGroupId);
            if (sequenceGroup == null)
            {
                throw new EntityNotFoundException("SequenceGroup", sequenceGroupId);
            }
            
            // Find the sequence-group association
            var sequenceGroupSequence = sequenceGroup.SequenceGroupSequences
                .FirstOrDefault(sgs => 
                    sgs.SequenceGroupId == sequenceGroupId && 
                    sgs.SequenceId == sequenceId);

            if (sequenceGroupSequence == null)
            {
                _logger.LogWarning("Sequence {SequenceId} not found in group {GroupId}", 
                    sequenceId, sequenceGroupId);
                return false;
            }

            // Remove the association
            // todo: does this work as expected? test 
            sequenceGroup.SequenceGroupSequences.Remove(sequenceGroupSequence);
            await _sequenceGroupRepository.SaveChangesAsync();
            
            _logger.LogInformation("Removed sequence {SequenceId} from group {GroupId}", 
                sequenceId, sequenceGroupId);
            
            return true;
        }
        catch (EntityNotFoundException)
        {
            _logger.LogError("Sequence group not found: {Id}", sequenceGroupId);
            throw;
        }
        catch (StorageProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing sequence {SequenceId} from group {GroupId}", 
                sequenceId, sequenceGroupId);
            return false;
        }
    }

    /// <summary>
    /// Validates a sequence group according to business rules
    /// </summary>
    /// <param name="sequenceGroupId">Identifier of the sequence group to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public async Task<bool> ValidateSequenceGroupAsync(string sequenceGroupId)
    {
        try
        {
            var sequenceGroup = await _sequenceGroupRepository.GetByIdAsync(sequenceGroupId);
            if (sequenceGroup == null)
            {
                _logger.LogError("Sequence group not found: {Id}", sequenceGroupId);
                throw new EntityNotFoundException("SequenceGroup", sequenceGroupId);
            }

            // Validate that the group has at least one sequence
            if (sequenceGroup.SequenceGroupSequences == null || sequenceGroup.SequenceGroupSequences.Count == 0)
            {
                _logger.LogWarning("Sequence group {Id} has no sequences", sequenceGroupId);
                return false;
            }

            // Get the ordered sequences
            var orderedSequences = await _sequenceGroupRepository.GetOrderedSequencesAsync(sequenceGroupId);
            
            // Validate that the ordered sequences form a continuous sequence (no gaps in order)
            var orderNumbers = orderedSequences.Keys.ToList();
            for (var i = 0; i < orderNumbers.Count - 1; i++)
            {
                if (orderNumbers[i+1] != orderNumbers[i] + 1)
                {
                    _logger.LogWarning("Sequence group {Id} has a gap in sequence ordering between positions {Pos1} and {Pos2}", 
                        sequenceGroupId, orderNumbers[i], orderNumbers[i+1]);
                    return false;
                }
            }

            // Additional validation logic can be added here as requirements evolve
            
            _logger.LogInformation("Validated sequence group {Id} successfully", sequenceGroupId);
            return true;
        }
        catch (EntityNotFoundException)
        {
            // Re-throw entity not found exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating sequence group {Id}", sequenceGroupId);
            return false;
        }
    }
}
