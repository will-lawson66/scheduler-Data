using Instrument.Data.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Instrument.Data.Services.Cleanup;

public class DatabaseCleanupService
{
    private readonly SchedulerDbContext _context;
    private readonly ILogger<DatabaseCleanupService> _logger;

    public DatabaseCleanupService(
        SchedulerDbContext context,
        ILogger<DatabaseCleanupService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Removes all data from all tables in the database
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        try
        {
            _logger.LogInformation("Beginning database cleanup operation");

            // Ensure database is created first
            await _context.Database.EnsureCreatedAsync();

            // Get all table names that exist in the database
            var existingTables = await GetExistingTablesAsync();
            
            // Clear data in the correct order to avoid foreign key constraints
            // Start with tables that reference others (child tables)
            await ClearTableIfExistsAsync("SequenceParameters", existingTables, 
                () => _context.SequenceParameters.ExecuteDeleteAsync());
            
            await ClearTableIfExistsAsync("SequenceGroupSequences", existingTables, 
                () => _context.SequenceGroupSequences.ExecuteDeleteAsync());
            
            await ClearTableIfExistsAsync("SequenceGroupCollectionSequenceGroups", existingTables, 
                () => _context.SequenceGroupCollectionSequenceGroups.ExecuteDeleteAsync());
            
            await ClearTableIfExistsAsync("RangeValues", existingTables, 
                () => _context.RangeValues.ExecuteDeleteAsync());

            // Then clear parent tables
            await ClearTableIfExistsAsync("Parameters", existingTables, 
                () => _context.Parameters.ExecuteDeleteAsync());
            
            await ClearTableIfExistsAsync("Sequences", existingTables, 
                () => _context.Sequences.ExecuteDeleteAsync());
            
            await ClearTableIfExistsAsync("SequenceGroups", existingTables, 
                () => _context.SequenceGroups.ExecuteDeleteAsync());
            
            await ClearTableIfExistsAsync("Ranges", existingTables, 
                () => _context.Ranges.ExecuteDeleteAsync());
            
            await ClearTableIfExistsAsync("Resources", existingTables, 
                () => _context.Resources.ExecuteDeleteAsync());
            
            await ClearTableIfExistsAsync("SequenceGroupCollectionBase", existingTables, 
                () => _context.SequenceGroupCollections.ExecuteDeleteAsync());

            _logger.LogInformation("Database cleanup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database cleanup");
            throw;
        }
    }

    /// <summary>
    /// Gets a list of existing table names in the database
    /// </summary>
    private async Task<HashSet<string>> GetExistingTablesAsync()
    {
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        try
        {
            // For SQLite, query the sqlite_master table to get existing tables
            if (_context.Database.IsSqlite())
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var tableName = reader.GetString(0);
                    tables.Add(tableName);
                }
            }
            else
            {
                // For other databases, you might need different queries
                // This is a fallback that assumes all tables exist
                tables.Add("SequenceParameters");
                tables.Add("SequenceGroupSequences");
                tables.Add("SequenceGroupCollectionSequenceGroups");
                tables.Add("RangeValues");
                tables.Add("Parameters");
                tables.Add("Sequences");
                tables.Add("SequenceGroups");
                tables.Add("Ranges");
                tables.Add("Resources");
                tables.Add("SequenceGroupCollectionBase");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve existing tables, assuming all tables exist");
            // If we can't determine which tables exist, assume they all do
            tables.Add("SequenceParameters");
            tables.Add("SequenceGroupSequences");
            tables.Add("SequenceGroupCollectionSequenceGroups");
            tables.Add("RangeValues");
            tables.Add("Parameters");
            tables.Add("Sequences");
            tables.Add("SequenceGroups");
            tables.Add("Ranges");
            tables.Add("Resources");
            tables.Add("SequenceGroupCollectionBase");
        }
        
        return tables;
    }

    /// <summary>
    /// Clears a table only if it exists in the database
    /// </summary>
    private async Task ClearTableIfExistsAsync(string tableName, HashSet<string> existingTables, Func<Task<int>> deleteAction)
    {
        if (!existingTables.Contains(tableName))
        {
            _logger.LogDebug("Table {TableName} does not exist, skipping cleanup", tableName);
            return;
        }

        try
        {
            var deletedCount = await deleteAction();
            _logger.LogDebug("Cleared {DeletedCount} records from table {TableName}", deletedCount, tableName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear table {TableName}, it may be empty or have constraints", tableName);
            
            // Alternative approach: Use raw SQL for more reliable deletion
            try
            {
                await _context.Database.ExecuteSqlRawAsync($"DELETE FROM [{tableName}]");
                _logger.LogDebug("Successfully cleared table {TableName} using raw SQL", tableName);
            }
            catch (Exception sqlEx)
            {
                _logger.LogWarning(sqlEx, "Failed to clear table {TableName} even with raw SQL", tableName);
                // Don't rethrow here - continue with other tables
            }
        }
    }
}