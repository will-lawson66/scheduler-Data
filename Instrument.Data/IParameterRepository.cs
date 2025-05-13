namespace Instrument.Data;

/// <summary>
/// Repository interface for parameters
/// </summary>
public interface IParameterRepository : IRepository<Instrument.Data.Entities.Parameter>
{
    /// <summary>
    /// Gets parameters for a sequence
    /// </summary>
    /// <param name="sequenceId">Sequence ID</param>
    Task<IEnumerable<Instrument.Data.Entities.Parameter?>> GetParametersForSequenceAsync(string sequenceId);
    
    /// <summary>
    /// Adds a parameter to a sequence
    /// </summary>
    /// <param name="parameterId">Parameter ID</param>
    /// <param name="sequenceId">Sequence ID</param>
    /// <param name="orderNumber">Order number</param>
    Task AddParameterToSequenceAsync(string parameterId, string sequenceId, int orderNumber);
    
    /// <summary>
    /// Removes a parameter from a sequence
    /// </summary>
    /// <param name="parameterId">Parameter ID</param>
    /// <param name="sequenceId">Sequence ID</param>
    Task RemoveParameterFromSequenceAsync(string parameterId, string sequenceId);
}