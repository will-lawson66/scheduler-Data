using Instrument.Scheduling.Data.Configuration;
using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Providers;
using Instrument.Scheduling.Data.Repository;
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
                break;
        }

        services.AddScoped<ISequenceRepository, SequenceRepository>();
        services.AddScoped<IParameterRepository, ParameterRepository>();
        services.AddScoped<IRangeRepository, RangeRepository>();
        services.AddScoped<IRangeValueRepository, RangeValueRepository>();
        services.AddScoped<IResourceRepository, ResourceRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        return services;
    }
}
