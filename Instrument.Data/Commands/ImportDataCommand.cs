using Instrument\.Data.Interfaces;
using Microsoft.Extensions.Logging;

namespace Instrument\.Data.Commands;

/// <summary>
/// Command to import data from JSON files
/// </summary>
public class ImportDataCommand : ICommand
{
    private readonly IJsonDataAdapter _jsonAdapter;
    private readonly ILogger<ImportDataCommand> _logger;

    /// <summary>
    /// Creates a new import data command
    /// </summary>
    /// <param name="jsonAdapter">JSON adapter for data import</param>
    /// <param name="logger">Logger</param>
    public ImportDataCommand(
        IJsonDataAdapter jsonAdapter,
        ILogger<ImportDataCommand> logger)
    {
        _jsonAdapter = jsonAdapter ?? throw new ArgumentNullException(nameof(jsonAdapter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the import command
    /// </summary>
    /// <param name="args">Command arguments (first argument should be the import path)</param>
    public async Task ExecuteAsync(string[] args)
    {
        if (args.Length < 1)
        {
            _logger.LogError("Import directory path is required");
            return;
        }

        string importPath = args[0]; // or config value JsonDataPath as fallback
        
        try
        {
            _logger.LogInformation("Importing data from {ImportPath}", importPath);
            await _jsonAdapter.ImportFromJsonAsync(importPath);
            _logger.LogInformation("Data successfully imported from {ImportPath}", importPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing data from JSON");
            throw;
        }
    }
}
