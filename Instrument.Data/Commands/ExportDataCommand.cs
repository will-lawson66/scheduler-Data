using Instrument\.Data.Interfaces;
using Microsoft.Extensions.Logging;

namespace Instrument\.Data.Commands;

/// <summary>
/// Command to export data to JSON files
/// </summary>
public class ExportDataCommand : ICommand
{
    private readonly IJsonDataAdapter _jsonAdapter;
    private readonly ILogger<ExportDataCommand> _logger;

    /// <summary>
    /// Creates a new export data command
    /// </summary>
    /// <param name="jsonAdapter">JSON adapter for data export</param>
    /// <param name="logger">Logger</param>
    public ExportDataCommand(
        IJsonDataAdapter jsonAdapter,
        ILogger<ExportDataCommand> logger)
    {
        _jsonAdapter = jsonAdapter ?? throw new ArgumentNullException(nameof(jsonAdapter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the export command
    /// </summary>
    /// <param name="args">Command arguments (first argument should be the export path)</param>
    public async Task ExecuteAsync(string[] args)
    {
        if (args.Length < 1)
        {
            _logger.LogError("Export directory path is required");
            return;
        }

        string exportPath = args[0]; // or config value
        
        try
        {
            _logger.LogInformation("Exporting data to {ExportPath}", exportPath);
            await _jsonAdapter.ExportToJsonAsync(exportPath);
            _logger.LogInformation("Data successfully exported to {ExportPath}", exportPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data to JSON");
            throw;
        }
    }
}
