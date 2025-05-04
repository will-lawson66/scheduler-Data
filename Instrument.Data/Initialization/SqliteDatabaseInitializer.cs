using Instrument.Data.Entities.Enums;
using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Range = Instrument.Data.Entities.Range;

namespace Instrument.Data.Initialization;

/// <summary>
/// Handles SQLite database initialization and migrations
/// </summary>
public class SqliteDatabaseInitializer : IDataInitializer
{
    private readonly SchedulerDbContext _context;
    private readonly ILogger<SqliteDatabaseInitializer> _logger;
    private readonly string _dbPath;

    public SqliteDatabaseInitializer(
        SchedulerDbContext context,
        ILogger<SqliteDatabaseInitializer> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Extract database path from connection string
        var connectionString = _context.Database.GetConnectionString();
        if (connectionString != null) _dbPath = ExtractDbPathFromConnectionString(connectionString);
    }

    /// <summary>
    /// Checks if the database file exists
    /// </summary>
    public Task<bool> ExistsAsync()
    {
        bool exists = File.Exists(_dbPath);
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

            if (migrationsList.Any())
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
    /// Seeds the database with default data if it's empty
    /// </summary>
    public async Task<bool> SeedDefaultDataAsync()
    {
        try
        {
            // Check if database is empty
            if (!await _context.Sequences.AnyAsync() &&
                !await _context.Parameters.AnyAsync() &&
                !await _context.Ranges.AnyAsync())
            {
                _logger.LogInformation("Seeding default data");

                // Add default range
                var defaultRange = new Range
                {
                    Id = "1",
                    Name = "Default Range",
                    Description = "Default range for testing"
                };

                await _context.Ranges.AddAsync(defaultRange);

                // Add default range values
                await _context.RangeValues.AddRangeAsync(
                    new RangeValue
                    {
                        Id = "1",
                        RangeId = defaultRange.Id,
                        Value = "Value1",
                        Name = "Default Value 1"
                    },
                    new RangeValue
                    {
                        Id = "2",
                        RangeId = defaultRange.Id,
                        Value = "Value2",
                        Name = "Default Value 2"
                    }
                );

                // Add a default parameter
                var defaultParameter = new Parameter
                {
                    Id = "1",
                    Name = "Default Parameter",
                    Type = ParameterType.StringType,
                    DefaultValue = "default"
                };

                await _context.Parameters.AddAsync(defaultParameter);

                // Add a default sequence
                var defaultSequence = new Sequence
                {
                    Id = "1",
                    Name = "Default Sequence",
                    Description = "Default sequence for testing",
                    WorstCaseTime = TimeSpan.FromMilliseconds(30000),
                    CanBeParallel = false
                };

                await _context.Sequences.AddAsync(defaultSequence);

                // Link parameter to sequence
                await _context.SequenceParameters.AddAsync(new SequenceParameter
                {
                    SequenceId = defaultSequence.Id,
                    ParameterId = defaultParameter.Id,
                    OrderNumber = 1
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Default data seeded successfully");
                return true;
            }

            _logger.LogInformation("Database already contains data, skipping seed");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed default data");
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
            bool exists = await ExistsAsync();
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
    private string ExtractDbPathFromConnectionString(string connectionString)
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
            if ((path.StartsWith("'") && path.EndsWith("'")) ||
                (path.StartsWith("\"") && path.EndsWith("\"")))
            {
                path = path.Substring(1, path.Length - 2);
            }

            return path;
        }

        return "scheduler.db"; // Default path
    }
}