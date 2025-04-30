using Instrument.Scheduling.Data.Configuration;
using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Initialization;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Providers;
using Instrument.Scheduling.Data.Repository;
using Instrument.Scheduling.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
                services.AddSingleton<IStorageProvider<Entities.Range>>(
                    sp => new JsonStorageProvider<Entities.Range>(config.JsonFilePath.Replace(".json", "_ranges.json")));
                services.AddSingleton<IStorageProvider<RangeValue>>(
                    sp => new JsonStorageProvider<RangeValue>(config.JsonFilePath.Replace(".json", "_range_values.json")));
                services.AddSingleton<IStorageProvider<Resource>>(
                    sp => new JsonStorageProvider<Resource>(config.JsonFilePath.Replace(".json", "_resources.json")));
                services.AddSingleton<IStorageProvider<SequenceGroup>>(
                    sp => new JsonStorageProvider<SequenceGroup>(config.JsonFilePath.Replace(".json", "_sequence_groups.json")));
                services.AddScoped<JsonDataCleanupService>();
                break;

            case StorageProviderType.SQLite:
                services.AddDbContext<SchedulerDbContext>(options =>
                    options.UseSqlite(config.ConnectionString));
                services.AddScoped<IStorageProvider<Sequence>, SqliteStorageProvider<Sequence>>();
                services.AddScoped<IStorageProvider<Parameter>, SqliteStorageProvider<Parameter>>();
                services.AddScoped<IStorageProvider<SequenceParameter>, SqliteSequenceParameterProvider>();
                services.AddScoped<IStorageProvider<Entities.Range>, SqliteStorageProvider<Entities.Range>>();
                services.AddScoped<IStorageProvider<RangeValue>, SqliteStorageProvider<RangeValue>>();
                services.AddScoped<IStorageProvider<Resource>, SqliteStorageProvider<Resource>>();
                services.AddScoped<IStorageProvider<SequenceGroup>, SqliteStorageProvider<SequenceGroup>>();
                services.AddScoped<DatabaseCleanupService>();

                break;

            case StorageProviderType.SqlServer:
                services.AddDbContext<SchedulerDbContext>(options =>
                    options.UseSqlServer(config.ConnectionString));
                services.AddScoped<IStorageProvider<Sequence>, SqlServerStorageProvider<Sequence>>();
                services.AddScoped<IStorageProvider<Parameter>, SqlServerStorageProvider<Parameter>>();
                services.AddScoped<IStorageProvider<SequenceParameter>, SqlServerSequenceParameterProvider>();
                services.AddScoped<IStorageProvider<Entities.Range>, SqlServerStorageProvider<Entities.Range>>();
                services.AddScoped<IStorageProvider<RangeValue>, SqlServerStorageProvider<RangeValue>>();
                services.AddScoped<IStorageProvider<Resource>, SqlServerStorageProvider<Resource>>();
                services.AddScoped<IStorageProvider<SequenceGroup>, SqlServerStorageProvider<SequenceGroup>>();
                services.AddScoped<DatabaseCleanupService>();
                break;
        }

        services.AddScoped<ISequenceRepository, SequenceRepository>();
        services.AddScoped<IParameterRepository, ParameterRepository>();
        services.AddScoped<IRangeRepository, RangeRepository>();
        services.AddScoped<IRangeValueRepository, RangeValueRepository>();
        services.AddScoped<IResourceRepository, ResourceRepository>();
        services.AddScoped<ISequenceGroupRepository, SequenceGroupRepository>();
        services.AddScoped<SequenceGroupService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        return services;
    }

    /// <summary>
    /// Adds data initialization and storage services to the service collection
    /// </summary>
    public static IServiceCollection AddSchedulerDataWithInitialization(
        this IServiceCollection services,
        StorageConfiguration config)
    {
        // Add data services
        services.AddSchedulerDataLayer(config);

        // Add initialization services
        services.AddDataInitialization();

        return services;
    }

    public static IServiceCollection AddDataInitialization(this IServiceCollection services)
    {
        // Register factory
        services.AddSingleton<DataInitializerFactory>();

        return services;
    }

    public static IServiceCollection AddCleanupServices(this IServiceCollection services)
    {
        // register the data cleanup services
        services.AddScoped<DatabaseCleanupService>();
        services.AddScoped<JsonDataCleanupService>();

        return services;
    }
}
