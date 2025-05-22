using Instrument.Data.Adapters;
using Instrument.Data.Adapters.Grpc;
using Instrument.Data.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Instrument.Data.Extensions;

/// <summary>
/// Extension methods for registering services in the dependency injection container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds gRPC client services to the DI container
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddGrpcClientServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register options
        services.Configure<GrpcAdapterOptions>(
            configuration.GetSection("GrpcAdapter"));
            
        // Register gRPC clients
        services.AddTransient<ISequenceGrpcClient, SequenceGrpcClient>();
        services.AddTransient<IParameterGrpcClient, ParameterGrpcClient>();
        services.AddTransient<IResourceGrpcClient, ResourceGrpcClient>();
        services.AddTransient<ISequenceGroupGrpcClient, SequenceGroupGrpcClient>();
        
        // Register adapter
        services.AddTransient<IGrpcDataAdapter, GrpcDataAdapter>();
        
        // Register API
        services.AddTransient<IDataImportApi, GrpcDataImportApi>();
        
        return services;
    }
}