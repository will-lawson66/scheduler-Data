using Instrument.Data.DataContext;
using Instrument.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Instrument.Data.Services
{
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

                // Clear data in the correct order to avoid foreign key constraints
                // Start with tables that reference others (child tables)
                await _context.SequenceParameters.ExecuteDeleteAsync();
                await _context.SequenceGroupSequences.ExecuteDeleteAsync();
                await _context.RangeValues.ExecuteDeleteAsync();

                // Then clear parent tables
                await _context.Parameters.ExecuteDeleteAsync();
                await _context.Sequences.ExecuteDeleteAsync();
                await _context.SequenceGroups.ExecuteDeleteAsync();
                await _context.Ranges.ExecuteDeleteAsync();
                await _context.Resources.ExecuteDeleteAsync();

                _logger.LogInformation("Database cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database cleanup");
                throw;
            }
        }
    }
}
