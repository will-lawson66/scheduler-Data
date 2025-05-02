using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Interfaces;
using Microsoft.Extensions.Logging;

namespace Instrument.Scheduling.Data.Services;
public class SequenceService
{
    private readonly ISequenceRepository _sequenceRepository;
    private readonly ILogger<SequenceService> _logger;

    public SequenceService(ISequenceRepository sequenceRepository,
        ILogger<SequenceService> logger)
    {
        _sequenceRepository = sequenceRepository ?? throw new ArgumentNullException(nameof(sequenceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Sequence?> GetSequenceAsync(string id)
    {
        _logger.LogInformation("Retrieving sequence with ID: {Id}", id);
        return await _sequenceRepository.GetByIdAsync(id);
    }

    public async Task CreateSequenceAsync(Sequence sequence)
    {
        if (sequence == null)
            throw new ArgumentNullException(nameof(sequence));

        _logger.LogInformation("Creating new sequence with ID: {Id}", sequence.Id);
        
        // Validate if a sequence with this ID already exists
        var existingSequence = await _sequenceRepository.GetByIdAsync(sequence.Id);
        if (existingSequence != null)
        {
            _logger.LogWarning("Sequence with ID {Id} already exists", sequence.Id);
            throw new SchedulerDataException($"Sequence with ID {sequence.Id} already exists");
        }

        try
        {
            await _sequenceRepository.AddAsync(sequence);
            await _sequenceRepository.SaveChangesAsync();
            _logger.LogInformation("Successfully created sequence with ID: {Id}", sequence.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sequence with ID: {Id}", sequence.Id);
            throw new StorageProviderException("CreateSequence", ex);
        }
    }

    // Original update method - kept for backward compatibility
    public async Task UpdateSequenceAsync(Sequence sequence)
    {
        if (sequence == null)
            throw new ArgumentNullException(nameof(sequence));

        _logger.LogInformation("Updating sequence with ID: {Id}", sequence.Id);
        
        // Validate if the sequence exists
        var existingSequence = await _sequenceRepository.GetByIdAsync(sequence.Id);
        if (existingSequence == null)
        {
            _logger.LogWarning("Sequence with ID {Id} does not exist", sequence.Id);
            throw new EntityNotFoundException("Sequence", sequence.Id);
        }

        try
        {
            await _sequenceRepository.UpdateAsync(sequence);
            await _sequenceRepository.SaveChangesAsync();
            _logger.LogInformation("Successfully updated sequence with ID: {Id}", sequence.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sequence with ID: {Id}", sequence.Id);
            throw new StorageProviderException("UpdateSequence", ex);
        }
    }
    
    // New property-based update method
    public async Task<Sequence> UpdateSequencePropertiesAsync(
        string id, 
        string? name = null, 
        TimeSpan? worstCaseTime = null, 
        string? description = null,
        bool? canBeParallel = null)
    {
        _logger.LogInformation("Updating properties for sequence with ID: {Id}", id);
        
        try
        {
            // Use the repository's new method
            var updatedSequence = await _sequenceRepository.UpdateSequencePropertiesAsync(
                id, name, worstCaseTime, description, canBeParallel);
                
            _logger.LogInformation("Successfully updated properties for sequence with ID: {Id}", id);
            return updatedSequence;
        }
        catch (EntityNotFoundException)
        {
            // Re-throw entity not found exceptions
            _logger.LogWarning("Sequence with ID {Id} does not exist", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating properties for sequence with ID: {Id}", id);
            throw new StorageProviderException("UpdateSequenceProperties", ex);
        }
    }

    public async Task DeleteSequenceAsync(string id)
    {
        _logger.LogInformation("Deleting sequence with ID: {Id}", id);
        
        // Validate if the sequence exists
        var existingSequence = await _sequenceRepository.GetByIdAsync(id);
        if (existingSequence == null)
        {
            _logger.LogWarning("Sequence with ID {Id} does not exist", id);
            throw new EntityNotFoundException("Sequence", id);
        }

        try
        {
            await _sequenceRepository.DeleteAsync(id);
            await _sequenceRepository.SaveChangesAsync();
            _logger.LogInformation("Successfully deleted sequence with ID: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sequence with ID: {Id}", id);
            throw new StorageProviderException("DeleteSequence", ex);
        }
    }

    public async Task<IEnumerable<Sequence>> GetAllSequencesAsync()
    {
        _logger.LogInformation("Retrieving all sequences");
        try
        {
            return await _sequenceRepository.GetAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all sequences");
            throw new StorageProviderException("GetAllSequences", ex);
        }
    }

    public async Task<IEnumerable<Sequence>> SearchSequencesAsync(
        Func<Sequence, bool> predicate)
    {
        _logger.LogInformation("Searching sequences with custom predicate");
        try
        {
            var queryable = await _sequenceRepository.GetQueryableAsync();
            var result = queryable.AsEnumerable().Where(predicate);
            _logger.LogInformation("Found {Count} sequences matching criteria", result.Count());
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching sequences");
            throw new StorageProviderException("SearchSequences", ex);
        }
    }
}
