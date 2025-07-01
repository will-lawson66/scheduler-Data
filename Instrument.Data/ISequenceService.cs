using Instrument.Data.DTOs;
using Instrument.Data.Entities;

namespace Instrument.Data;

public interface ISequenceService
{
    Task<Sequence?> GetSequenceByIdAsync(int id);
    Task<Sequence> CreateSequenceAsync(Sequence sequence);
    Task UpdateSequenceAsync(Sequence sequence);
    Task DeleteSequenceAsync(int id);
    Task<IEnumerable<Sequence>> GetAllSequencesAsync();

    /// <summary>
    /// Add a parameter to a sequence
    /// </summary>
    /// <param name="parameterId"></param>
    /// <param name="sequenceId"></param>
    /// <param name="orderNumber"></param>
    /// <returns></returns>
    Task AddParameterToSequenceAsync(int parameterId, int sequenceId, int orderNumber = 0);

    /// <summary>
    /// Remove a parameter from a sequence
    /// </summary>
    /// <param name="parameterId"></param>
    /// <param name="sequenceId"></param>
    /// <returns></returns>
    Task RemoveParameterFromSequenceAsync(int parameterId, int sequenceId);

    /// <summary>
    /// Get a sequence by Id with its associated parameters.
    /// </summary>
    /// <param name="sequenceId"></param>
    /// <returns></returns>
    Task<Sequence?> GetSequenceWithParametersAsync(int sequenceId);
    
    // DTO-based methods
    /// <summary>
    /// Gets a single sequence as DTO without identity keys
    /// </summary>
    /// <param name="name">Optional name filter - if null, returns first sequence</param>
    /// <returns>SequenceDTO if found, null otherwise</returns>
    Task<SequenceDTO?> GetSequenceAsync(string? name = null);
    
    /// <summary>
    /// Gets sequences as DTOs without identity keys
    /// </summary>
    /// <param name="name">Optional name filter - if null, all sequences are returned</param>
    /// <returns>Collection of SequenceDTOs</returns>
    Task<IEnumerable<SequenceDTO>> GetSequencesAsync(string? name = null);
}
