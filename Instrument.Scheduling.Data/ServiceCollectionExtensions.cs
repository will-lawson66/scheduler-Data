using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Providers;
using Instrument.Scheduling.Data.Repository;

namespace Instrument.Scheduling.Data;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulerDataLayer(this IServiceCollection services, 
        StorageConfiguration config)
    {
        switch (config.Provider)
        {
            case StorageProviderType.Json:
                // Register JSON providers
                services.AddSingleton<IStorageProvider<Sequence>>(
                    sp => new JsonStorageProvider<Sequence>(config.JsonFilePath));
                services.AddSingleton<IStorageProvider<Parameter>>(
                    sp => new JsonStorageProvider<Parameter>(config.JsonFilePath.Replace(".json", "_parameters.json")));
                services.AddSingleton<IStorageProvider<SequenceParameter>>(
                    sp => new JsonStorageProvider<SequenceParameter>(config.JsonFilePath.Replace(".json", "_sequence_parameters.json")));
                break;
                
            case StorageProviderType.SQLite:
                services.AddDbContext<SchedulerDbContext>(options =>
                    options.UseSqlite(config.ConnectionString));
                services.AddScoped<IStorageProvider<Sequence>, SqliteStorageProvider<Sequence>>();
                services.AddScoped<IStorageProvider<Parameter>, SqliteStorageProvider<Parameter>>();
                services.AddScoped<IStorageProvider<SequenceParameter>, SqliteSequenceParameterProvider>();
                break;
                
            case StorageProviderType.SqlServer:
                services.AddDbContext<SchedulerDbContext>(options =>
                    options.UseSqlServer(config.ConnectionString));
                services.AddScoped<IStorageProvider<Sequence>, SqliteStorageProvider<Sequence>>();
                services.AddScoped<IStorageProvider<Parameter>, SqliteStorageProvider<Parameter>>();
                services.AddScoped<IStorageProvider<SequenceParameter>, SqliteSequenceParameterProvider>();
                break;
        }

        services.AddScoped<ISequenceRepository, SequenceRepository>();
        services.AddScoped<IParameterRepository, ParameterRepository>();
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
