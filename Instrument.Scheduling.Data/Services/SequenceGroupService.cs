using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Instrument.Scheduling.Data.Services;

/// <summary>
/// Service for managing sequence groups
/// </summary>
public class SequenceGroupService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SchedulerDbContext _dbContext;
    private readonly ILogger<SequenceGroupService> _logger;

    public SequenceGroupService(
        IUnitOfWork unitOfWork,
        SchedulerDbContext dbContext,
        ILogger<SequenceGroupService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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
            throw new ArgumentException("Sequence group ID cannot be empty", nameof(id));
            
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Sequence group name cannot be empty", nameof(name));

        // Check if ID already exists
        var existingGroup = await _unitOfWork.SequenceGroups.GetByIdAsync(id);
        if (existingGroup != null)
            throw new SchedulerDataException($"Sequence group with ID {id} already exists");

        // Create and save the sequence group
        var sequenceGroup = new SequenceGroup
        {
            Id = id,
            Name = name,
            Description = description
        };

        try
        {
            await _unitOfWork.SequenceGroups.AddAsync(sequenceGroup);
            await _unitOfWork.SaveChangesAsync();

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
    /// Adds a sequence to a sequence group in a specific order
    /// </summary>
    /// <param name="sequenceGroupId">Identifier of the sequence group</param>
    /// <param name="sequenceId">Identifier of the sequence to add</param>
    /// <param name="order">Order/position of the sequence within the group</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> AddSequenceToGroupAsync(string sequenceGroupId, string sequenceId, int order)
    {
        _logger.LogInformation("Adding sequence {SequenceId} to group {GroupId} at order {Order}", 
            sequenceId, sequenceGroupId, order);

        try
        {
            // Get the sequence group
            var sequenceGroup = await _unitOfWork.SequenceGroups.GetByIdAsync(sequenceGroupId);
            if (sequenceGroup == null)
            {
                _logger.LogError("Sequence group not found: {Id}", sequenceGroupId);
                throw new EntityNotFoundException("SequenceGroup", sequenceGroupId);
            }

            // Get the sequence
            var sequence = await _unitOfWork.SequenceDefinitions.GetByIdAsync(sequenceId);
            if (sequence == null)
            {
                _logger.LogError("Sequence not found: {Id}", sequenceId);
                throw new EntityNotFoundException("Sequence", sequenceId);
            }

            // Add the sequence to the group
            await sequenceGroup.AddSequenceAsync(sequence, order, _dbContext);
            _logger.LogInformation("Successfully added sequence {SequenceId} to group {GroupId} at order {Order}", 
                sequenceId, sequenceGroupId, order);
            
            return true;
        }
        catch (EntityNotFoundException)
        {
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
    /// <returns>A sorted list of sequences by their order</returns>
    public async Task<SortedList<int, Sequence>> GetOrderedSequencesAsync(string sequenceGroupId)
    {
        _logger.LogInformation("Getting ordered sequences for group: {Id}", sequenceGroupId);
        try
        {
            // Validate if the sequence group exists
            var sequenceGroup = await _unitOfWork.SequenceGroups.GetByIdAsync(sequenceGroupId);
            if (sequenceGroup == null)
            {
                throw new EntityNotFoundException("SequenceGroup", sequenceGroupId);
            }
            
            return await _unitOfWork.SequenceGroups.GetOrderedSequencesAsync(sequenceGroupId);
        }
        catch (EntityNotFoundException)
        {
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
    /// Gets all sequence groups
    /// </summary>
    /// <returns>Collection of all sequence groups</returns>
    public async Task<IEnumerable<SequenceGroup>> GetAllSequenceGroupsAsync()
    {
        try
        {
            return await _unitOfWork.SequenceGroups.GetAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all sequence groups");
            throw new StorageProviderException("GetAllSequenceGroups", ex);
        }
    }

    /// <summary>
    /// Gets a sequence group by its identifier
    /// </summary>
    /// <param name="id">Identifier of the sequence group</param>
    /// <returns>The sequence group if found, null otherwise</returns>
    public async Task<SequenceGroup?> GetSequenceGroupByIdAsync(string id)
    {
        try
        {
            return await _unitOfWork.SequenceGroups.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sequence group by ID: {Id}", id);
            throw new StorageProviderException("GetSequenceGroupById", ex);
        }
    }

    /// <summary>
    /// Gets a sequence group with its associated sequences
    /// </summary>
    /// <param name="id">Identifier of the sequence group</param>
    /// <returns>The sequence group with sequences if found, null otherwise</returns>
    public async Task<SequenceGroup?> GetSequenceGroupWithSequencesAsync(string id)
    {
        try
        {
            var result = await _unitOfWork.SequenceGroups.GetWithSequencesAsync(id);
            if (result == null)
            {
                _logger.LogWarning("Sequence group not found: {Id}", id);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sequence group with sequences by ID: {Id}", id);
            throw new StorageProviderException("GetSequenceGroupWithSequences", ex);
        }
    }

    /// <summary>
    /// Deletes a sequence group
    /// </summary>
    /// <param name="id">Identifier of the sequence group to delete</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> DeleteSequenceGroupAsync(string id)
    {
        try
        {
            // Validate if the sequence group exists
            var sequenceGroup = await _unitOfWork.SequenceGroups.GetByIdAsync(id);
            if (sequenceGroup == null)
            {
                throw new EntityNotFoundException("SequenceGroup", id);
            }
            
            await _unitOfWork.SequenceGroups.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch (EntityNotFoundException)
        {
            // Re-throw entity not found exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sequence group {Id}", id);
            return false;
        }
    }

    /// <summary>
    /// Removes a sequence from a sequence group
    /// </summary>
    /// <param name="sequenceGroupId">Identifier of the sequence group</param>
    /// <param name="sequenceId">Identifier of the sequence to remove</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> RemoveSequenceFromGroupAsync(string sequenceGroupId, string sequenceId)
    {
        try
        {
            // Validate if the sequence group exists
            var sequenceGroup = await _unitOfWork.SequenceGroups.GetByIdAsync(sequenceGroupId);
            if (sequenceGroup == null)
            {
                throw new EntityNotFoundException("SequenceGroup", sequenceGroupId);
            }
            
            // Find the sequence-group association
            var sequenceGroupSequence = await _dbContext.SequenceGroupSequences
                .FirstOrDefaultAsync(sgs => 
                    sgs.SequenceGroupId == sequenceGroupId && 
                    sgs.SequenceId == sequenceId);

            if (sequenceGroupSequence == null)
            {
                _logger.LogWarning("Sequence {SequenceId} not found in group {GroupId}", 
                    sequenceId, sequenceGroupId);
                return false;
            }

            // Remove the association
            _dbContext.SequenceGroupSequences.Remove(sequenceGroupSequence);
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Removed sequence {SequenceId} from group {GroupId}", 
                sequenceId, sequenceGroupId);
            
            return true;
        }
        catch (EntityNotFoundException)
        {
            // Re-throw entity not found exceptions
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
    /// Changes the order of a sequence in a sequence group
    /// </summary>
    /// <param name="sequenceGroupId">Identifier of the sequence group</param>
    /// <param name="sequenceId">Identifier of the sequence to reorder</param>
    /// <param name="newOrder">New order/position for the sequence</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> ReorderSequenceInGroupAsync(string sequenceGroupId, string sequenceId, int newOrder)
    {
        try
        {
            // Validate if the sequence group exists
            var sequenceGroup = await _unitOfWork.SequenceGroups.GetByIdAsync(sequenceGroupId);
            if (sequenceGroup == null)
            {
                throw new EntityNotFoundException("SequenceGroup", sequenceGroupId);
            }
            
            // Find the sequence-group association
            var sequenceGroupSequence = await _dbContext.SequenceGroupSequences
                .FirstOrDefaultAsync(sgs => 
                    sgs.SequenceGroupId == sequenceGroupId && 
                    sgs.SequenceId == sequenceId);

            if (sequenceGroupSequence == null)
            {
                _logger.LogWarning("Sequence {SequenceId} not found in group {GroupId}", 
                    sequenceId, sequenceGroupId);
                return false;
            }

            // Get the current order
            int oldOrder = sequenceGroupSequence.Order;
            
            // If the order is the same, do nothing
            if (oldOrder == newOrder)
                return true;

            // Remove the association
            _dbContext.SequenceGroupSequences.Remove(sequenceGroupSequence);
            
            // Get the sequence
            var sequence = await _unitOfWork.SequenceDefinitions.GetByIdAsync(sequenceId);
            if (sequence == null)
            {
                _logger.LogError("Sequence not found: {Id}", sequenceId);
                throw new EntityNotFoundException("Sequence", sequenceId);
            }

            // Add the sequence with the new order
            await sequenceGroup.AddSequenceAsync(sequence, newOrder, _dbContext);
            
            _logger.LogInformation("Reordered sequence {SequenceId} in group {GroupId} from {OldOrder} to {NewOrder}", 
                sequenceId, sequenceGroupId, oldOrder, newOrder);
            
            return true;
        }
        catch (EntityNotFoundException)
        {
            // Re-throw entity not found exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering sequence {SequenceId} in group {GroupId}", 
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
            var sequenceGroup = await _unitOfWork.SequenceGroups.GetWithSequencesAsync(sequenceGroupId);
            if (sequenceGroup == null)
            {
                _logger.LogError("Sequence group not found: {Id}", sequenceGroupId);
                throw new EntityNotFoundException("SequenceGroup", sequenceGroupId);
            }

            // Validate that the group has at least one sequence
            if (sequenceGroup.SequenceGroupSequences == null || !sequenceGroup.SequenceGroupSequences.Any())
            {
                _logger.LogWarning("Sequence group {Id} has no sequences", sequenceGroupId);
                return false;
            }

            // Get the ordered sequences
            var orderedSequences = await _unitOfWork.SequenceGroups.GetOrderedSequencesAsync(sequenceGroupId);
            
            // Validate that the ordered sequences form a continuous sequence (no gaps in order)
            var orderNumbers = orderedSequences.Keys.ToList();
            for (int i = 0; i < orderNumbers.Count - 1; i++)
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
