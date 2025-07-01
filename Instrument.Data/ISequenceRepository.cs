using Instrument.Data.Entities;

namespace Instrument.Data;

/// <summary>
/// Repository interface for sequences
/// </summary>
public interface ISequenceRepository : IRepository<Sequence>
{
    /// <summary>
    /// Gets a sequence with its parameters
    /// </summary>
    /// <param name="sequenceId">Sequence ID</param>
    Task<Sequence?> GetSequenceWithParametersAsync(int sequenceId);
    
    /// <summary>
    /// Gets sequences by name
    /// </summary>
    /// <param name="name">Sequence name</param>
    Task<IEnumerable<Sequence>> GetSequencesByPartialNameAsync(string name);
    
    /// <summary>
    /// Adds a parameter to a sequence
    /// </summary>
    /// <param name="parameterId">Parameter ID</param>
    /// <param name="sequenceId">Sequence ID</param>
    /// <param name="orderNumber">Order number</param>
    Task AddParameterToSequenceAsync(int parameterId, int sequenceId, int orderNumber = 0);

    /// <summary>
    /// Removes a parameter from a sequence
    /// </summary>
    /// <param name="parameterId">Parameter ID</param>
    /// <param name="sequenceId">Sequence ID</param>
    Task RemoveParameterFromSequenceAsync(int parameterId, int sequenceId);
    
    /// <summary>
    /// Gets sequences with their parameters filtered by name
    /// </summary>
    /// <param name="name">Optional name filter - if null, all sequences are returned</param>
    /// <returns>Collection of Sequences with parameters loaded</returns>
    Task<IEnumerable<Sequence>> GetSequencesWithParametersAsync(string? name = null);
    
    /// <summary>
    /// Gets the ordered parameters for a specific sequence
    /// </summary>
    /// <param name="sequenceId">Sequence ID</param>
    /// <returns>Ordered list of parameters</returns>
    Task<IEnumerable<Parameter>> GetOrderedParametersAsync(int sequenceId);
}
