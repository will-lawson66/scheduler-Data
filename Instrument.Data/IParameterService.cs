using Instrument.Data.Entities;

namespace Instrument.Data;

public interface IParameterService
{
    Task<Parameter?> GetParameterByIdAsync(string id);
    Task CreateParameterAsync(Parameter parameter);
    Task UpdateParameterAsync(Parameter parameter);
    Task DeleteParameterAsync(string id);
    Task<IEnumerable<Parameter>> GetAllParametersAsync();

    /// <summary>
    /// Get parameters for a specific sequence
    /// </summary>
    /// <param name="sequenceId"></param>
    /// <returns></returns>
    Task<IEnumerable<Parameter?>> GetParametersForSequenceAsync(string sequenceId);

    /// <summary>
    /// Add a parameter to a sequence
    /// </summary>
    /// <param name="parameterId"></param>
    /// <param name="sequenceId"></param>
    /// <param name="orderNumber"></param>
    /// <returns></returns>
    Task AddParameterToSequenceAsync(string parameterId, string sequenceId, int orderNumber);

    /// <summary>
    /// Remove a parameter from a sequence
    /// </summary>
    /// <param name="parameterId"></param>
    /// <param name="sequenceId"></param>
    /// <returns></returns>
    Task RemoveParameterFromSequenceAsync(string parameterId, string sequenceId);

    /// <summary>
    /// Validate parameter value against constraints
    /// </summary>
    /// <param name="parameter"></param>
    /// <param name="value"></param>
    void ValidateParameterValue(Parameter parameter, string value);
}
