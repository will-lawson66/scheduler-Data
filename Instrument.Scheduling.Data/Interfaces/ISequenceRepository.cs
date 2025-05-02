using Instrument.Scheduling.Data.Entities;

namespace Instrument.Scheduling.Data.Interfaces;

/// <summary>
/// Repository interface for sequences
/// </summary>
public interface ISequenceRepository : IRepository<Sequence>
{
    /// <summary>
    /// Gets a sequence with its parameters
    /// </summary>
    /// <param name="id">Sequence ID</param>
    Task<Sequence?> GetSequenceWithParametersAsync(string id);
    
    /// <summary>
    /// Gets sequences by name
    /// </summary>
    /// <param name="name">Sequence name</param>
    Task<IEnumerable<Sequence>> GetSequencesByNameAsync(string name);
    
    /// <summary>
    /// Removes a parameter from a sequence
    /// </summary>
    /// <param name="parameterId">Parameter ID</param>
    /// <param name="sequenceId">Sequence ID</param>
    Task RemoveParameterFromSequenceAsync(string parameterId, string sequenceId);
    
    /// <summary>
    /// Gets sequences by their IDs
    /// </summary>
    /// <param name="ids">Sequence IDs</param>
    Task<IEnumerable<Sequence>> GetSequencesByIdsAsync(IEnumerable<string> ids);
    
    /// <summary>
    /// Updates a sequence with specific property changes
    /// </summary>
    /// <param name="id">Sequence ID</param>
    /// <param name="name">Optional new name</param>
    /// <param name="worstCaseTime">Optional new worst case time</param>
    /// <param name="description">Optional new description</param>
    /// <param name="canBeParallel">Optional new canBeParallel value</param>
    /// <returns>The updated sequence</returns>
    Task<Sequence> UpdateSequencePropertiesAsync(
        string id, 
        string? name = null, 
        TimeSpan? worstCaseTime = null, 
        string? description = null,
        bool? canBeParallel = null);
}
