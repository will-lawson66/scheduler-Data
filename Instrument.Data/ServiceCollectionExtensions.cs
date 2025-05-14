using Instrument.Data.Adapters;
using Instrument.Data.Commands;
using Instrument.Data.Configuration;
using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Instrument.Data.Initialization;
using Instrument.Data;
using Instrument.Data.Repository;
using Instrument.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Instrument.Data;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulerDataLayer(this IServiceCollection services, 
        StorageConfiguration config)
    {
        // Ensure logging services are available
        services.AddLogging(builder => builder
            .SetMinimumLevel(LogLevel.Information)
            .AddConsole());

        switch (config.Provider)
        {

            case StorageProviderType.SQLite:
                services.AddDbContext<SchedulerDbContext>(options =>
                    options
                        .UseSqlite(config.ConnectionString)
                        .LogTo(Console.WriteLine, LogLevel.Warning));
                services.AddScoped<DatabaseCleanupService>();

                break;

            case StorageProviderType.SqlServer:
                services.AddDbContext<SchedulerDbContext>(options =>
                    options
                        .UseSqlServer(config.ConnectionString)
                        .LogTo(Console.WriteLine, LogLevel.Warning));
                services.AddScoped<DatabaseCleanupService>();
                break;
        }

        // Register the JSON adapter
        services.AddScoped<IJsonDataAdapter, Adapters.JsonDataAdapter>();
        
        // Register repositories
        services.AddScoped<ISequenceRepository, SequenceRepository>();
        services.AddScoped<IParameterRepository, ParameterRepository>();
        services.AddScoped<IRangeRepository, RangeRepository>();
        services.AddScoped<IRangeValueRepository, RangeValueRepository>();
        services.AddScoped<IResourceRepository, ResourceRepository>();
        services.AddScoped<ISequenceGroupRepository, SequenceGroupRepository>();
        services.AddScoped(typeof(ISequenceGroupCollectionRepository<>), typeof(SequenceGroupCollectionRepository<>));

        // Services
        services.AddScoped<IParameterService, ParameterService>();
        services.AddScoped<IRangeService, RangeService>();
        services.AddScoped<IRangeValueService, RangeValueService>();
        services.AddScoped<IResourceService, ResourceService>();
        services.AddScoped<ISequenceGroupService, SequenceGroupService>();
        services.AddScoped(typeof(ISequenceGroupCollectionService<>), typeof(SequenceGroupCollectionService<>));
        services.AddScoped<ISequenceService, SequenceService>();

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

    /// <summary>
    /// Adds data initialization services to the service collection
    /// </summary>
    public static IServiceCollection AddDataInitialization(this IServiceCollection services)
    {
        // Register the factory and database initializers
        services.AddSingleton<DataInitializerFactory>();
        services.AddScoped<SqliteDatabaseInitializer>();

        return services;
    }

    public static IServiceCollection AddCleanupServices(this IServiceCollection services)
    {
        // register the data cleanup services
        services.AddScoped<DatabaseCleanupService>((sp) => new DatabaseCleanupService(
            sp.GetRequiredService<SchedulerDbContext>(),
            sp.GetRequiredService<ILogger<DatabaseCleanupService>>()));

        return services;
    }
    
    /// <summary>
    /// Adds JSON import/export commands to the service collection
    /// </summary>
    public static IServiceCollection AddJsonCommands(this IServiceCollection services)
    {
        services.AddScoped<ICommand, ExportDataCommand>((sp) => new ExportDataCommand(
            sp.GetRequiredService<IJsonDataAdapter>(),
            sp.GetRequiredService<ILogger<ExportDataCommand>>()));
            
        services.AddScoped<ICommand, ImportDataCommand>((sp) => new ImportDataCommand(
            sp.GetRequiredService<IJsonDataAdapter>(),
            sp.GetRequiredService<ILogger<ImportDataCommand>>()));

        return services;
    }
}
