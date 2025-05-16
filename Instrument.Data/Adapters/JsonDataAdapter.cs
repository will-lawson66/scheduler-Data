using System.Text.Json;
using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Instrument.Data.Adapters;

/// <summary>
/// Adapter for importing from and exporting to JSON files
/// </summary>
public class JsonDataAdapter : IJsonDataAdapter
{
    private readonly SchedulerDbContext _dbContext;
    private readonly ILogger<JsonDataAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonDataAdapter(
        SchedulerDbContext dbContext,
        ILogger<JsonDataAdapter> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    /// <summary>
    /// Exports all data to JSON files in the specified directory
    /// </summary>
    public async Task ExportToJsonAsync(string directoryPath)
    {
        _logger.LogInformation("Starting export to JSON in directory: {DirectoryPath}", directoryPath);
        
        // Ensure directory exists
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            _logger.LogInformation("Created export directory: {DirectoryPath}", directoryPath);
        }

        // Export each entity type to a separate JSON file
        await ExportEntityAsync(_dbContext.Sequences, Path.Combine(directoryPath, "sequences.json"));
        await ExportEntityAsync(_dbContext.Parameters, Path.Combine(directoryPath, "parameters.json"));
        await ExportEntityAsync(_dbContext.SequenceParameters, Path.Combine(directoryPath, "sequence_parameters.json"));
        await ExportEntityAsync(_dbContext.Ranges, Path.Combine(directoryPath, "ranges.json"));
        await ExportEntityAsync(_dbContext.RangeValues, Path.Combine(directoryPath, "range_values.json"));
        await ExportEntityAsync(_dbContext.Resources, Path.Combine(directoryPath, "resources.json"));
        await ExportEntityAsync(_dbContext.SequenceGroups, Path.Combine(directoryPath, "sequence_groups.json"));
        await ExportEntityAsync(_dbContext.SequenceGroupSequences, Path.Combine(directoryPath, "sequence_group_sequences.json"));
        
        _logger.LogInformation("Data successfully exported to JSON in directory: {DirectoryPath}", directoryPath);
    }

    /// <summary>
    /// Helper method to export an entity set to a JSON file
    /// </summary>
    private async Task ExportEntityAsync<T>(DbSet<T> dbSet, string filePath) where T : class
    {
        try
        {
            var entities = await dbSet.ToListAsync();
            var json = JsonSerializer.Serialize(entities, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
            _logger.LogInformation("Exported {Count} {EntityType} to {FilePath}", 
                entities.Count, typeof(T).Name, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting {EntityType} to {FilePath}", 
                typeof(T).Name, filePath);
            throw;
        }
    }

    /// <summary>
    /// Imports data from JSON files in the specified directory
    /// </summary>
    public async Task ImportFromJsonAsync(string directoryPath)
    {
        _logger.LogInformation("Starting import from JSON in directory: {DirectoryPath}", directoryPath);
        
        if (!Directory.Exists(directoryPath))
        {
            _logger.LogError("Import directory does not exist: {DirectoryPath}", directoryPath);
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        // First, clear existing data
        await ClearDatabaseAsync();

        // Import each entity type from JSON files in a specific order to respect dependencies
        await ImportEntityAsync<Sequence>(Path.Combine(directoryPath, "sequences.json"), _dbContext.Sequences);
        await ImportEntityAsync<SequenceGroupCollectionBase>(Path.Combine(directoryPath, "sequence_group_collections.json"), _dbContext.SequenceGroupCollections);
        await ImportEntityAsync<Entities.Range>(Path.Combine(directoryPath, "ranges.json"), _dbContext.Ranges);
        await ImportEntityAsync<Resource>(Path.Combine(directoryPath, "resources.json"), _dbContext.Resources);
        await ImportEntityAsync<Parameter>(Path.Combine(directoryPath, "parameters.json"), _dbContext.Parameters);
        await ImportEntityAsync<RangeValue>(Path.Combine(directoryPath, "range_values.json"), _dbContext.RangeValues);
        await ImportEntityAsync<SequenceParameter>(Path.Combine(directoryPath, "sequence_parameters.json"), _dbContext.SequenceParameters);
        await ImportEntityAsync<SequenceGroup>(Path.Combine(directoryPath, "sequence_groups.json"), _dbContext.SequenceGroups);
        await ImportEntityAsync<SequenceGroupSequence>(Path.Combine(directoryPath, "sequence_group_sequences.json"), _dbContext.SequenceGroupSequences);
        await ImportEntityAsync<SequenceGroupCollectionSequenceGroup>(Path.Combine(directoryPath, "sequence_group_sequences.json"), _dbContext.SequenceGroupCollectionSequenceGroups);

        // Save all changes to database
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("Data successfully imported from JSON in directory: {DirectoryPath}", directoryPath);
    }

    /// <summary>
    /// Helper method to import entities from a JSON file
    /// </summary>
    private async Task ImportEntityAsync<T>(string filePath, DbSet<T> dbSet) where T : class
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Import file not found, skipping: {FilePath}", filePath);
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var entities = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);
            
            if (entities != null && entities.Count == 0)
            {
                await dbSet.AddRangeAsync(entities);
                _logger.LogInformation("Imported {Count} {EntityType} from {FilePath}", 
                    entities.Count, typeof(T).Name, filePath);
            }
            else
            {
                _logger.LogWarning("No {EntityType} found in {FilePath}", typeof(T).Name, filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing {EntityType} from {FilePath}", 
                typeof(T).Name, filePath);
            throw;
        }
    }

    /// <summary>
    /// Clears all data from the database in the correct order to respect foreign key constraints
    /// </summary>
    private async Task ClearDatabaseAsync()
    {
        _logger.LogInformation("Clearing database before import");
        
        try
        {
            // Clear data in reverse order of dependencies
            _dbContext.SequenceGroupCollectionSequenceGroups.RemoveRange(_dbContext.SequenceGroupCollectionSequenceGroups);
            _dbContext.SequenceGroupSequences.RemoveRange(_dbContext.SequenceGroupSequences);
            _dbContext.SequenceParameters.RemoveRange(_dbContext.SequenceParameters);
            _dbContext.RangeValues.RemoveRange(_dbContext.RangeValues);
            _dbContext.Parameters.RemoveRange(_dbContext.Parameters);
            _dbContext.Ranges.RemoveRange(_dbContext.Ranges);
            _dbContext.Resources.RemoveRange(_dbContext.Resources);
            _dbContext.Sequences.RemoveRange(_dbContext.Sequences);
            _dbContext.SequenceGroupCollections.RemoveRange(_dbContext.SequenceGroupCollections);
            _dbContext.SequenceGroups.RemoveRange(_dbContext.SequenceGroups);

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Database cleared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing database before import");
            throw;
        }
    }
}
