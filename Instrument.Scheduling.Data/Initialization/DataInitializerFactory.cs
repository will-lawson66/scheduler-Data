using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Configuration;

namespace Instrument.Scheduling.Data.Initialization
{
    /// <summary>
    /// Factory for creating database initializers
    /// </summary>
    public class DataInitializerFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DataInitializerFactory> _logger;

        public DataInitializerFactory(
            IServiceProvider serviceProvider,
            ILogger<DataInitializerFactory> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates an appropriate data initializer based on the storage configuration
        /// </summary>
        public IDataInitializer CreateInitializer(StorageConfiguration config)
        {
            _logger.LogInformation("Creating data initializer for storage provider: {Provider}", config.Provider);

            switch (config.Provider)
            {
                case StorageProviderType.SQLite:
                    return CreateSqliteInitializer();

                case StorageProviderType.SqlServer:
                    throw new NotImplementedException("SQL Server initializer not implemented");

                default:
                    throw new ArgumentException($"Unsupported storage provider: {config.Provider}");
            }
        }

        /// <summary>
        /// Creates a SQLite database initializer
        /// </summary>
        private IDataInitializer CreateSqliteInitializer()
        {
            var context = _serviceProvider.GetRequiredService<SchedulerDbContext>();
            var logger = _serviceProvider.GetRequiredService<ILogger<SqliteDatabaseInitializer>>();

            return new SqliteDatabaseInitializer(context, logger);
        }
    }
}