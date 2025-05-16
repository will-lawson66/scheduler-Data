using Instrument.Data.Entities;

namespace Instrument.Data;

public interface IParameterService
{
    Task<Parameter?> GetParameterByIdAsync(int id);
    Task<Parameter> CreateParameterAsync(Parameter parameter);
    Task UpdateParameterAsync(Parameter parameter);
    Task DeleteParameterAsync(int id);
    Task<IEnumerable<Parameter>> GetAllParametersAsync();

    /// <summary>
    /// Validate parameter value against constraints
    /// </summary>
    /// <param name="parameter"></param>
    /// <param name="value"></param>
    void ValidateParameterValue(Parameter parameter, string value);
}