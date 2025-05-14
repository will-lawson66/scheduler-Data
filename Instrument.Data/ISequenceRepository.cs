namespace Instrument.Data;

/// <summary>
/// Repository interface for sequences
/// </summary>
public interface ISequenceRepository : IRepository<Entities.Sequence>
{
    /// <summary>
    /// Gets a sequence with its parameters
    /// </summary>
    /// <param name="id">Sequence ID</param>
    Task<Entities.Sequence?> GetSequenceWithParametersAsync(int id);
    
    /// <summary>
    /// Gets sequences by name
    /// </summary>
    /// <param name="name">Sequence name</param>
    Task<IEnumerable<Entities.Sequence>> GetSequencesByPartialNameAsync(string name);
    
    /// <summary>
    /// Removes a parameter from a sequence
    /// </summary>
    /// <param name="parameterId">Parameter ID</param>
    /// <param name="sequenceId">Sequence ID</param>
    Task RemoveParameterFromSequenceAsync(int parameterId, int sequenceId);
}
