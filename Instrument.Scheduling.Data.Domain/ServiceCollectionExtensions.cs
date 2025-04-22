using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Scheduler.DataLayer.Data;
using Scheduler.DataLayer.Entities;
using Scheduler.DataLayer.Interfaces;
using Scheduler.DataLayer.Providers;
using Scheduler.DataLayer.Repositories;

namespace Scheduler.DataLayer.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSchedulerDataLayer(this IServiceCollection services, 
            StorageConfiguration config)
        {
            switch (config.Provider)
            {
                case StorageProviderType.Json:
                    services.AddSingleton<IStorageProvider<SequenceDefinition>>(
                        sp => new JsonStorageProvider<SequenceDefinition>(config.JsonFilePath));
                    break;
                    
                case StorageProviderType.SQLite:
                    services.AddDbContext<SchedulerDbContext>(options =>
                        options.UseSqlite(config.ConnectionString));
                    services.AddScoped<IStorageProvider<SequenceDefinition>, SqliteStorageProvider<SequenceDefinition>>();
                    break;
                    
                case StorageProviderType.SqlServer:
                    services.AddDbContext<SchedulerDbContext>(options =>
                        options.UseSqlServer(config.ConnectionString));
                    services.AddScoped<IStorageProvider<SequenceDefinition>, SqliteStorageProvider<SequenceDefinition>>();
                    break;
            }

            services.AddScoped<ISequenceDefinitionRepository, SequenceDefinitionRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            
            return services;
        }
    }

    public class StorageConfiguration
    {
        public StorageProviderType Provider { get; set; } = StorageProviderType.Json;
        public string JsonFilePath { get; set; } = "sequence_definitions.json";
        public string ConnectionString { get; set; } = string.Empty;
    }

    public enum StorageProviderType
    {
        Json,
        SQLite,
        SqlServer
    }
}
