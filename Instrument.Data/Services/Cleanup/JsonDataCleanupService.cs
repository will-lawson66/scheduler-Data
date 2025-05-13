using Instrument.Data;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Instrument.Data.Services;

public class JsonDataCleanupService
{
    private readonly ILogger<JsonDataCleanupService> _logger;
    private readonly string _dataDirectory;

    public JsonDataCleanupService(
        ILogger<JsonDataCleanupService> logger,
        string dataDirectory = "./data/json")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dataDirectory = dataDirectory;
    }

    /// <summary>
    /// Clears all JSON data files
    /// </summary>
    public void ClearAllData()
    {
        try
        {
            _logger.LogInformation("Clearing all JSON data files in {Directory}", _dataDirectory);

            if (!Directory.Exists(_dataDirectory))
            {
                _logger.LogWarning("JSON data directory does not exist: {Directory}", _dataDirectory);
                return;
            }

            // Get all JSON files in the directory
            var jsonFiles = Directory.GetFiles(_dataDirectory, "*.json");

            foreach (var file in jsonFiles)
            {
                _logger.LogInformation("Clearing JSON file: {File}", Path.GetFileName(file));

                // Replace content with empty array
                File.WriteAllText(file, "[]");
            }

            _logger.LogInformation("All JSON data files cleared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing JSON data files");
            throw;
        }
    }
}