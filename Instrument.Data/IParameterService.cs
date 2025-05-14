using Instrument.Data.Entities;

namespace Instrument.Data;

public interface IParameterService
{
    Task<Parameter?> GetParameterByIdAsync(int id);
    Task CreateParameterAsync(Parameter parameter);
    Task UpdateParameterAsync(Parameter parameter);
    Task DeleteParameterAsync(int id);
    Task<IEnumerable<Parameter>> GetAllParametersAsync();

    /// <summary>
    /// Get parameters for a specific sequence
    /// </summary>
    /// <param name="sequenceId"></param>
    /// <returns></returns>
    Task<IEnumerable<Parameter?>> GetParametersForSequenceAsync(int sequenceId);

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
    /// Validate parameter value against constraints
    /// </summary>
    /// <param name="parameter"></param>
    /// <param name="value"></param>
    void ValidateParameterValue(Parameter parameter, string value);
}