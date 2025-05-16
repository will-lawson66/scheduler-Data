using Instrument.Data.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Instrument.Data.Initialization;

/// <summary>
/// Handles SQLite database initialization and migrations
/// </summary>
public class SqliteDatabaseInitializer : IDataInitializer
{
    private readonly SchedulerDbContext _context;
    private readonly ILogger<SqliteDatabaseInitializer> _logger;
    private readonly string _dbPath = "";

    public SqliteDatabaseInitializer(
        SchedulerDbContext context,
        ILogger<SqliteDatabaseInitializer> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Extract database path from connection string
        var connectionString = _context.Database.GetConnectionString();
        if (connectionString != null)
        {
            _dbPath = ExtractDbPathFromConnectionString(connectionString);
        }
    }

    /// <summary>
    /// Checks if the database file exists
    /// </summary>
    public Task<bool> ExistsAsync()
    {
        var exists = File.Exists(_dbPath);
        _logger.LogInformation("SQLite database {Exists} at {Path}",
            exists ? "exists" : "does not exist", _dbPath);
        return Task.FromResult(exists);
    }

    /// <summary>
    /// Initializes the database, creating it if it doesn't exist
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing SQLite database at {Path}", _dbPath);

            // Ensure the directory exists
            var directory = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create database if it doesn't exist
            await _context.Database.EnsureCreatedAsync();

            _logger.LogInformation("SQLite database initialization completed successfully");
            
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SQLite database at {Path}", _dbPath);
            throw;
        }
    }

    /// <summary>
    /// Applies any pending migrations
    /// </summary>
    public async Task<bool> MigrateAsync()
    {
        try
        {
            // Get pending migrations
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            var migrationsList = pendingMigrations.ToList();

            if (migrationsList.Count == 0)
            {
                _logger.LogInformation("Applying {Count} pending migrations", migrationsList.Count);

                // Apply migrations
                await _context.Database.MigrateAsync();

                _logger.LogInformation("Migrations applied successfully");
                return true;
            }

            _logger.LogInformation("No pending migrations found");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply migrations");
            throw;
        }
    }

    /// <summary>
    /// Gets a status message about the database
    /// </summary>
    public async Task<string> GetStatusMessageAsync()
    {
        try
        {
            var exists = await ExistsAsync();
            if (!exists)
            {
                return $"SQLite database does not exist at {_dbPath}";
            }

            var sequenceCount = await _context.Sequences.CountAsync();
            var parameterCount = await _context.Parameters.CountAsync();
            var rangeCount = await _context.Ranges.CountAsync();

            return $"SQLite database at {_dbPath} contains {sequenceCount} sequences, " +
                   $"{parameterCount} parameters, and {rangeCount} ranges.";
        }
        catch (Exception ex)
        {
            return $"Error accessing SQLite database: {ex.Message}";
        }
    }

    /// <summary>
    /// Extracts the database file path from a SQLite connection string
    /// </summary>
    private static string ExtractDbPathFromConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return "scheduler.db"; // Default path
        }

        // Simple parsing for "Data Source=path" format
        const string dataSourcePrefix = "Data Source=";
        if (connectionString.StartsWith(dataSourcePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var path = connectionString.Substring(dataSourcePrefix.Length).Trim();

            // Handle quoted paths
            if ((path.StartsWith('\'') && path.EndsWith('\'')) ||
                (path.StartsWith('\"') && path.EndsWith('\"')))
            {
                path = path.Substring(1, path.Length - 2);
            }

            return path;
        }

        return "scheduler.db"; // Default path
    }
}