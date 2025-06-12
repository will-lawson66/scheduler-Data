using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Instrument.Execution.Grpc.Configuration;

namespace Instrument.Execution.Grpc.FakeService;

/// <summary>
/// Configuration options for the fake execution configuration service.
/// </summary>
public class FakeServiceOptions
{
    /// <summary>
    /// Artificial delay to simulate network latency.
    /// </summary>
    public TimeSpan ResponseDelay { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Probability of simulating a service failure (0.0 to 1.0).
    /// </summary>
    public double FailureRate { get; set; } = 0.0;

    /// <summary>
    /// Path to the JSON file containing sample data.
    /// </summary>
    public string DataSourcePath { get; set; } = "SampleData.json";

    /// <summary>
    /// Whether to enable concurrency testing features.
    /// </summary>
    public bool EnableConcurrencyTesting { get; set; } = true;

    /// <summary>
    /// Maximum number of concurrent requests to handle.
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 10;

    /// <summary>
    /// Whether to use detailed logging for debugging.
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = true;

    /// <summary>
    /// Scenario to use for testing (Default, Minimal, Complex, ErrorProne).
    /// </summary>
    public string TestScenario { get; set; } = "Default";
}

/// <summary>
/// Extension methods for registering the fake execution configuration service.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the fake execution configuration service with default options.
    /// </summary>
    public static IServiceCollection AddFakeExecutionConfigurationService(
        this IServiceCollection services)
    {
        return services.AddFakeExecutionConfigurationService(_ => { });
    }

    /// <summary>
    /// Registers the fake execution configuration service with custom options.
    /// </summary>
    public static IServiceCollection AddFakeExecutionConfigurationService(
        this IServiceCollection services,
        Action<FakeServiceOptions> configureOptions)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        // Configure options
        services.Configure(configureOptions);

        // Register supporting services
        services.TryAddSingleton<ConfigurationBuilder>();
        services.TryAddSingleton<FakeDataProvider>();
        services.TryAddSingleton<DelaySimulator>();
        services.TryAddSingleton<ResponseBuilder>();

        // Register the main service
        services.TryAddScoped<IExecutionConfigurationService, FakeExecutionConfigurationService>();

        return services;
    }

    /// <summary>
    /// Registers the fake service for testing with specific test scenario.
    /// </summary>
    public static IServiceCollection AddFakeExecutionConfigurationServiceForTesting(
        this IServiceCollection services,
        string testScenario = "Default",
        TimeSpan? responseDelay = null,
        double failureRate = 0.0)
    {
        return services.AddFakeExecutionConfigurationService(options =>
        {
            options.TestScenario = testScenario;
            options.ResponseDelay = responseDelay ?? TimeSpan.Zero;
            options.FailureRate = failureRate;
            options.EnableDetailedLogging = true;
            options.EnableConcurrencyTesting = true;
        });
    }

    /// <summary>
    /// Registers the fake service with error simulation for resilience testing.
    /// </summary>
    public static IServiceCollection AddFakeExecutionConfigurationServiceWithErrors(
        this IServiceCollection services,
        double failureRate = 0.1,
        TimeSpan? responseDelay = null)
    {
        return services.AddFakeExecutionConfigurationService(options =>
        {
            options.FailureRate = failureRate;
            options.ResponseDelay = responseDelay ?? TimeSpan.FromMilliseconds(100);
            options.EnableDetailedLogging = true;
            options.TestScenario = "ErrorProne";
        });
    }

    /// <summary>
    /// Registers the fake service for load testing with realistic delays.
    /// </summary>
    public static IServiceCollection AddFakeExecutionConfigurationServiceForLoadTesting(
        this IServiceCollection services,
        TimeSpan? responseDelay = null,
        int maxConcurrentRequests = 100)
    {
        return services.AddFakeExecutionConfigurationService(options =>
        {
            options.ResponseDelay = responseDelay ?? TimeSpan.FromMilliseconds(500);
            options.MaxConcurrentRequests = maxConcurrentRequests;
            options.EnableConcurrencyTesting = true;
            options.EnableDetailedLogging = false; // Reduce logging overhead for load testing
            options.TestScenario = "Complex";
        });
    }
}