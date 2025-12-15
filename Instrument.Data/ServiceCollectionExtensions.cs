using System.Text.Json;

namespace Instrument.Data;
using Configuration;
using DataContext;
using Grpc;
using Initialization;
using Orchestration;
using Orchestration.ConfigurationImport;
using Orchestration.ConfigurationImport.Steps;
using Repository;
using Services;
using Services.Cleanup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

public static class ServiceCollectionExtensions
{


    public static IServiceCollection AddSchedulingDataLayer(
        this IServiceCollection services,
        IConfiguration config)
    {
        var storageConfiguration = config.GetSection("StorageConfiguration").Get<StorageConfiguration>() ?? new StorageConfiguration();

        // Configure sequence group options
        services.Configure<SequenceGroupOptions>(config.GetSection(SequenceGroupOptions.SectionName));
        
        // Configure JSON options for proper serialization
        //services.ConfigureAll<JsonOptions>(options =>
        //{
        //    options.SerializerOptions.Converters.Add(new TechnologyJsonConverter());
        //    options.SerializerOptions.Converters.Add(new TimeSpanJsonConverter());
        //    options.SerializerOptions.PropertyNameCaseInsensitive = true;
        //    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        //});

        // Ensure logging services are available
        services.AddLogging(builder => builder
            .SetMinimumLevel(LogLevel.Information)
            .AddConsole());

        switch (storageConfiguration.Provider)
        {

            case StorageProviderType.SQLite:
                services.AddDbContext<SchedulerDbContext>(options =>
                    options
                        .UseSqlite(storageConfiguration.ConnectionString)
                        .LogTo(Console.WriteLine, LogLevel.Warning));
                services.AddScoped<DatabaseCleanupService>();

                break;

            case StorageProviderType.SqlServer:
                services.AddDbContext<SchedulerDbContext>(options =>
                    options
                        .UseSqlServer(storageConfiguration.ConnectionString)
                        .LogTo(Console.WriteLine, LogLevel.Warning));
                services.AddScoped<DatabaseCleanupService>();
                break;
        }

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
        services.AddScoped<ISequenceGroupConfigurationService, SequenceGroupConfigurationService>();
        services.AddScoped<ISequenceGroupImportExportService, SequenceGroupImportExportService>();
        services.AddScoped<ISequenceImportExportService, SequenceImportExportService>();
        services.AddScoped(typeof(ISequenceGroupCollectionService<>), typeof(SequenceGroupCollectionService<>));
        services.AddScoped<ISequenceService, SequenceService>();

        return services;
    }

    /// <summary>
    /// Adds data initialization and storage services to the service collection
    /// </summary>
    public static IServiceCollection AddSchedulingDataLayerWithInitialization(
        this IServiceCollection services,
        IConfiguration config)
    {
        // Add data services
        services.AddSchedulingDataLayer(config);

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
    /// Add gRPC gateway configuration to container.
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureGrpcGateway(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        return serviceCollection.Configure<GrpcGatewayOptions>(configuration.GetSection($"{nameof(GrpcGatewayOptions)}"));
    }

    /// <summary>
    /// Register gRPC gateway for scheduling application.
    /// </summary>
    public static IServiceCollection AddSchedulingGrpcGateway(this IServiceCollection services)
    {
        // Gateway services
        services.AddSingleton<IRetryPolicy, ExponentialBackoffRetryPolicy>();
        services.AddScoped<IGrpcGateway, GrpcGateway>();

        return services;
    }

    /// <summary>
    /// Add process management orchestration.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddSchedulingProcessManager(this IServiceCollection services)
    {
        // Orchestration services
        services.AddScoped<IProcessManager<ConfigurationImportRequest, ConfigurationImportResult>, ConfigurationImportManager>();

        // Orchestration steps
        services.AddScoped<IOrchestrationStep, GetConfigurationStep>();
        services.AddScoped<IOrchestrationStep, ImportResourcesStep>();
        services.AddScoped<IOrchestrationStep, ImportSequencesStep>();
        services.AddScoped<IOrchestrationStep, InitializeDatabaseStep>();
        services.AddScoped<IOrchestrationStep, TruncateDataStep>();
        services.AddScoped<IOrchestrationStep, ValidateRequestStep>();

        return services;
    }
}
