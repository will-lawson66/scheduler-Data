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
}
