using Instrument.Data.DTOs;
using Instrument.Data.Entities;
using Instrument.Data.Exceptions;
using Instrument.Data.Repository;
using Microsoft.Extensions.Logging;
using System.Reflection.Metadata;

namespace Instrument.Data.Services;
public class SequenceService : ISequenceService
{
    private readonly ISequenceRepository _sequenceRepository;
    private readonly ILogger<SequenceService> _logger;

    public SequenceService(ISequenceRepository sequenceRepository,
        ILogger<SequenceService> logger)
    {
        _sequenceRepository = sequenceRepository ?? throw new ArgumentNullException(nameof(sequenceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Sequence?> GetSequenceByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving sequence with ID: {Id}", id);
        return await _sequenceRepository.GetByIdAsync(id);
    }

    public async Task<Sequence> CreateSequenceAsync(Sequence sequence)
    {
        if (sequence == null)
        {
            throw new ArgumentNullException(nameof(sequence));
        }

        _logger.LogInformation("Creating new sequence with ID: {Id}", sequence.Id);
        
        try
        {
            await _sequenceRepository.AddAsync(sequence);
            await _sequenceRepository.SaveChangesAsync();
            _logger.LogInformation("Successfully created sequence with ID: {Id}", sequence.Id);
            return sequence;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sequence with ID: {Id}", sequence.Id);
            throw new StorageProviderException("CreateSequence", ex);
        }
    }

    public async Task UpdateSequenceAsync(Sequence sequence)
    {
        if (sequence == null)
        {
            throw new ArgumentNullException(nameof(sequence));
        }

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
    
    public async Task DeleteSequenceAsync(int id)
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

    /// <inheritdoc />
    public async Task AddParameterToSequenceAsync(int parameterId, int sequenceId, int orderNumber)
    {
        _logger.LogInformation("Adding parameter {ParameterId} to sequence {SequenceId} with order {OrderNumber}",
            parameterId, sequenceId, orderNumber);

        // Validate parameter exists
        var parameter = await _sequenceRepository.GetByIdAsync(parameterId);
        if (parameter == null)
        {
            _logger.LogWarning("Parameter with ID {Id} does not exist", parameterId);
            throw new EntityNotFoundException("Parameter", parameterId);
        }

        try
        {
            await _sequenceRepository.AddParameterToSequenceAsync(parameterId, sequenceId, orderNumber);
            await _sequenceRepository.SaveChangesAsync();
            _logger.LogInformation("Successfully added parameter {ParameterId} to sequence {SequenceId}",
                parameterId, sequenceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding parameter {ParameterId} to sequence {SequenceId}",
                parameterId, sequenceId);
            throw new StorageProviderException("AddParameterToSequence", ex);
        }
    }

    /// <inheritdoc />
    public async Task RemoveParameterFromSequenceAsync(int parameterId, int sequenceId)
    {
        _logger.LogInformation("Removing parameter {ParameterId} from sequence {SequenceId}",
            parameterId, sequenceId);

        try
        {
            await _sequenceRepository.RemoveParameterFromSequenceAsync(parameterId, sequenceId);
            await _sequenceRepository.SaveChangesAsync();
            _logger.LogInformation("Successfully removed parameter {ParameterId} from sequence {SequenceId}",
                parameterId, sequenceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing parameter {ParameterId} from sequence {SequenceId}",
                parameterId, sequenceId);
            throw new StorageProviderException("RemoveParameterFromSequence", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Sequence?> GetSequenceWithParametersAsync(int sequenceId)
    {
        _logger.LogInformation("Retrieving sequence with ID: {Id}", sequenceId);

        try
        {
            _logger.LogInformation("Sequence retrieved with ID: {Id}", sequenceId);
            return await _sequenceRepository.GetSequenceWithParametersAsync(sequenceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sequence with Id: {Id}", sequenceId);
            throw new StorageProviderException("GetSequenceWithParameters", ex);
        }
    }

    /// <summary>
    /// Gets a single sequence by name, returns DTO without identity keys
    /// </summary>
    /// <param name="name">Optional name filter - if null, returns first sequence</param>
    /// <returns>SequenceDTO if found, null otherwise</returns>
    public async Task<SequenceDTO?> GetSequenceAsync(string? name = null)
    {
        try
        {
            var sequences = await _sequenceRepository.GetSequencesWithParametersAsync(name);
            var sequence = sequences.FirstOrDefault();
            
            return sequence != null ? await ConvertToDTOAsync(sequence) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sequence with name: {Name}", name);
            throw new StorageProviderException("GetSequence", ex);
        }
    }

    /// <summary>
    /// Gets sequences by name, returns DTOs without identity keys
    /// </summary>
    /// <param name="name">Optional name filter - if null, all sequences are returned</param>
    /// <returns>Collection of SequenceDTOs</returns>
    public async Task<IEnumerable<SequenceDTO>> GetSequencesAsync(string? name = null)
    {
        try
        {
            var sequences = await _sequenceRepository.GetSequencesWithParametersAsync(name);
            var sequenceDtos = new List<SequenceDTO>();
            
            foreach (var sequence in sequences)
            {
                var dto = await ConvertToDTOAsync(sequence);
                sequenceDtos.Add(dto);
            }
            
            return sequenceDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sequences with name: {Name}", name);
            throw new StorageProviderException("GetSequences", ex);
        }
    }

    /// <summary>
    /// Converts a Sequence entity to SequenceDTO with parameters projected and ordered
    /// </summary>
    /// <param name="sequence">The Sequence entity</param>
    /// <returns>SequenceDTO with parameters projected from SequenceParameters</returns>
    private async Task<SequenceDTO> ConvertToDTOAsync(Sequence sequence)
    {
        // Get ordered parameters for this sequence
        var orderedParameters = await _sequenceRepository.GetOrderedParametersAsync(sequence.Id);

        return new SequenceDTO
        {
            Name = sequence.Name,
            Order = null, // Order is contextual (set by parent SequenceGroup)
            Parameters = orderedParameters
        };
    }
}