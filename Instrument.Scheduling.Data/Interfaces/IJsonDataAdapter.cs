namespace Instrument.Scheduling.Data.Interfaces;

/// <summary>
/// Interface for JSON data import/export adapter
/// </summary>
public interface IJsonDataAdapter
{
    /// <summary>
    /// Exports all data to JSON files in the specified directory
    /// </summary>
    /// <param name="directoryPath">Directory to export to</param>
    Task ExportToJsonAsync(string directoryPath);

    /// <summary>
    /// Imports data from JSON files in the specified directory
    /// </summary>
    /// <param name="directoryPath">Directory to import from</param>
    Task ImportFromJsonAsync(string directoryPath);
}
