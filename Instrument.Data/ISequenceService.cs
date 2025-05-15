using Instrument.Data.Entities;

namespace Instrument.Data;

public interface ISequenceService
{
    Task<Sequence?> GetSequenceByIdAsync(int id);
    Task CreateSequenceAsync(Sequence sequence);
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
    /// Gets a sequence with its parameters
    /// </summary>
    /// <param name="sequenceId">Sequence ID</param>
    //Task<Sequence?> GetSequenceWithParametersAsync(int sequenceId);

    /// <summary>
    /// Gets sequences by name
    /// </summary>
    /// <param name="name">Sequence name</param>
    //Task<IEnumerable<Sequence>> GetSequencesByPartialNameAsync(string name);
}
