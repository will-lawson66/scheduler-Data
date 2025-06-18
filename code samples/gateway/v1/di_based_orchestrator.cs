// =====================================================================================
// DI-BASED CONFIGURATION ORCHESTRATOR (NO CHANNEL MANAGEMENT)
// Leverages existing DI-registered gRPC clients
// =====================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;
using System.ServiceModel;
using System.Diagnostics;

// =====================================================================================
// 1. EXECUTION SERVICE gRPC CONTRACTS (SAME AS BEFORE)
// =====================================================================================

namespace Instrument.Data.Execution.Contracts
{
    [ServiceContract]
    public interface IExecutionConfigurationService
    {
        [OperationContract]
        Task<GetCurrentConfigurationResponse> GetCurrentConfigurationAsync(
            GetCurrentConfigurationRequest request, 
            CancellationToken cancellationToken = default);

        [OperationContract]
        Task<GetSequenceConfigurationResponse> GetSequenceConfigurationAsync(
            GetSequenceConfigurationRequest request, 
            CancellationToken cancellationToken = default);

        [OperationContract]
        Task<GetResourceConfigurationResponse> GetResourceConfigurationAsync(
            GetResourceConfigurationRequest request, 
            CancellationToken cancellationToken = default);
    }

    // DTOs remain the same as previous implementation...
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record GetCurrentConfigurationRequest(bool IncludeSequences = true);

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record GetSequenceConfigurationRequest(string Key);

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record GetResourceConfigurationRequest(string Key);

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record GetCurrentConfigurationResponse(
        GuidRequestId RequestId,
        ExecutionConfigurationContract? Configuration,
        IReadOnlyCollection<GrpcErrorContract> Errors
    );

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record GetSequenceConfigurationResponse(
        GuidRequestId RequestId,
        ExecutionSequenceContract? Sequence,
        IReadOnlyCollection<GrpcErrorContract> Errors
    );

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record GetResourceConfigurationResponse(
        GuidRequestId RequestId,
        ExecutionResourceContract? Resource,
        IReadOnlyCollection<GrpcErrorContract> Errors
    );

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record ExecutionConfigurationContract(
        int StartingPeriod,
        int RolloverPeriod,
        TimeSpan PeriodSpan,
        double PeriodAcceleration,
        IReadOnlyCollection<ExecutionSequenceContract> Sequences
    );

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record ExecutionSequenceContract(
        string Key,
        TimeSpan WorstCaseTime,
        string ExecutionMethod,
        string ScriptKey,
        IReadOnlyCollection<ExecutionResourceContract> Resources,
        IReadOnlyCollection<SequenceParameterTypeContract> Parameters
    );

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record ExecutionResourceContract(
        string Key,
        bool HasScriptingInterface,
        string ScriptingInterface
    );

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record SequenceParameterTypeContract(
        string ParameterName,
        ParameterType ParameterType
    );

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record GuidRequestId(string Lo, string Hi);

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record GrpcErrorContract(string Message, string Code);

    public enum ParameterType
    {
        StringType,
        IntegerType,
        DecimalType,
        BooleanType,
        ArrayType,
        EnumType
    }
}

// =====================================================================================
// 2. SIMPLIFIED RESILIENT gRPC SERVICE WRAPPER
// =====================================================================================

namespace Instrument.Data.Services.Grpc
{
    /// <summary>
    /// Resilient wrapper for gRPC services using DI-registered clients
    /// Provides retry, circuit breaker, caching without channel management
    /// </summary>
    public interface IResilientGrpcService
    {
        /// <summary>
        /// Execute a gRPC call with resilience features
        /// </summary>
        Task<TResult> ExecuteAsync<TService, TResult>(
            Func<TService, CancellationToken, Task<TResult>> operation,
            string operationName = null,
            CancellationToken cancellationToken = default)
            where TService : class;

        /// <summary>
        /// Test if a gRPC service is available
        /// </summary>
        Task<bool> TestServiceAsync<TService>(CancellationToken cancellationToken = default)
            where TService : class;

        /// <summary>
        /// Clear cache for specific operation
        /// </summary>
        void ClearCache(string operationName = null);
    }

    /// <summary>
    /// Options for resilient gRPC service
    /// </summary>
    public class ResilientGrpcOptions
    {
        public const string SectionName = "ResilientGrpc";

        public bool EnableCaching { get; set; } = true;
        public int CacheExpiryMinutes { get; set; } = 15;
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 1000;
        public double RetryBackoffMultiplier { get; set; } = 2.0;
        public int CircuitBreakerThreshold { get; set; } = 5;
        public int CircuitBreakerTimeoutSeconds { get; set; } = 30;
        public int TimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Implementation of resilient gRPC service using DI-registered clients
    /// </summary>
    public class ResilientGrpcService : IResilientGrpcService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ResilientGrpcService> _logger;
        private readonly ResilientGrpcOptions _options;
        private readonly SemaphoreSlim _semaphore;
        
        // Simple circuit breaker state per service type
        private readonly ConcurrentDictionary<Type, CircuitBreakerState> _circuitStates = new();
        private readonly ConcurrentDictionary<Type, DateTime> _lastFailureTime = new();
        private readonly ConcurrentDictionary<Type, int> _failureCounts = new();

        public ResilientGrpcService(
            IServiceProvider serviceProvider,
            IMemoryCache cache,
            ILogger<ResilientGrpcService> logger,
            IOptions<ResilientGrpcOptions> options)
        {
            _serviceProvider = serviceProvider;
            _cache = cache;
            _logger = logger;
            _options = options.Value;
            _semaphore = new SemaphoreSlim(10, 10); // Simple concurrency control
        }

        public async Task<TResult> ExecuteAsync<TService, TResult>(
            Func<TService, CancellationToken, Task<TResult>> operation,
            string operationName = null,
            CancellationToken cancellationToken = default)
            where TService : class
        {
            var serviceType = typeof(TService);
            var cacheKey = $"{serviceType.Name}_{operationName}";
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug("Executing gRPC operation {ServiceType}.{OperationName}", 
                serviceType.Name, operationName);

            // Check cache first
            if (_options.EnableCaching && !string.IsNullOrEmpty(operationName))
            {
                if (_cache.TryGetValue(cacheKey, out TResult cachedResult))
                {
                    _logger.LogDebug("Cache hit for {ServiceType}.{OperationName}", 
                        serviceType.Name, operationName);
                    return cachedResult;
                }
            }

            // Check circuit breaker
            if (IsCircuitOpen(serviceType))
            {
                var timeSinceLastFailure = DateTime.UtcNow - _lastFailureTime.GetValueOrDefault(serviceType);
                if (timeSinceLastFailure < TimeSpan.FromSeconds(_options.CircuitBreakerTimeoutSeconds))
                {
                    // Try to return cached data as fallback
                    var fallbackKey = $"{cacheKey}_fallback";
                    if (_cache.TryGetValue(fallbackKey, out TResult fallbackResult))
                    {
                        _logger.LogWarning("Circuit breaker open for {ServiceType}, returning fallback data", 
                            serviceType.Name);
                        return fallbackResult;
                    }

                    throw new InvalidOperationException($"Service {serviceType.Name} is temporarily unavailable (circuit breaker open)");
                }
                else
                {
                    // Reset circuit breaker
                    ResetCircuitBreaker(serviceType);
                }
            }

            await _semaphore.WaitAsync(cancellationToken);
            
            try
            {
                var result = await ExecuteWithRetryAsync(operation, serviceType, cancellationToken);

                // Cache successful result
                if (_options.EnableCaching && !string.IsNullOrEmpty(operationName) && result != null)
                {
                    var cacheExpiry = TimeSpan.FromMinutes(_options.CacheExpiryMinutes);
                    _cache.Set(cacheKey, result, cacheExpiry);
                    
                    // Also store as fallback with longer expiry
                    _cache.Set($"{cacheKey}_fallback", result, cacheExpiry.Multiply(4));
                }

                // Reset failure count on success
                _failureCounts.TryRemove(serviceType, out _);

                _logger.LogInformation("Successfully executed {ServiceType}.{OperationName} in {ElapsedMs}ms",
                    serviceType.Name, operationName, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                RecordFailure(serviceType);
                _logger.LogError(ex, "Failed to execute {ServiceType}.{OperationName} after retries",
                    serviceType.Name, operationName);
                throw;
            }
            finally
            {
                _semaphore.Release();
                stopwatch.Stop();
            }
        }

        public async Task<bool> TestServiceAsync<TService>(CancellationToken cancellationToken = default)
            where TService : class
        {
            try
            {
                var service = _serviceProvider.GetRequiredService<TService>();
                // For basic connectivity test, we just check if we can resolve the service
                // For more sophisticated testing, you could call a health check method
                return service != null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Service test failed for {ServiceType}", typeof(TService).Name);
                return false;
            }
        }

        public void ClearCache(string operationName = null)
        {
            if (string.IsNullOrEmpty(operationName))
            {
                _logger.LogInformation("Cache clear requested for all operations");
                // For IMemoryCache, we can't clear all entries easily
                // You might want to implement a custom cache wrapper for this
            }
            else
            {
                _cache.Remove(operationName);
                _cache.Remove($"{operationName}_fallback");
                _logger.LogDebug("Cleared cache for operation: {OperationName}", operationName);
            }
        }

        #region Private Helper Methods

        private async Task<TResult> ExecuteWithRetryAsync<TService, TResult>(
            Func<TService, CancellationToken, Task<TResult>> operation,
            Type serviceType,
            CancellationToken cancellationToken)
            where TService : class
        {
            Exception lastException = null;

            for (int attempt = 0; attempt <= _options.MaxRetryAttempts; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        var delay = _options.RetryDelayMs * Math.Pow(_options.RetryBackoffMultiplier, attempt - 1);
                        _logger.LogDebug("Retry attempt {Attempt} for {ServiceType} after {DelayMs}ms", 
                            attempt, serviceType.Name, delay);
                        await Task.Delay(TimeSpan.FromMilliseconds(delay), cancellationToken);
                    }

                    // Get service from DI container
                    var service = _serviceProvider.GetRequiredService<TService>();

                    // Execute with timeout
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

                    return await operation(service, linkedCts.Token);
                }
                catch (Exception ex) when (IsRetriableException(ex) && attempt < _options.MaxRetryAttempts)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "Attempt {Attempt} failed for {ServiceType} with retriable error", 
                        attempt + 1, serviceType.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Non-retriable error on attempt {Attempt} for {ServiceType}", 
                        attempt + 1, serviceType.Name);
                    throw;
                }
            }

            throw lastException ?? new InvalidOperationException($"All retry attempts failed for {serviceType.Name}");
        }

        private static bool IsRetriableException(Exception ex)
        {
            return ex switch
            {
                RpcException rpcEx => rpcEx.StatusCode == StatusCode.DeadlineExceeded ||
                                     rpcEx.StatusCode == StatusCode.Unavailable ||
                                     rpcEx.StatusCode == StatusCode.Internal ||
                                     rpcEx.StatusCode == StatusCode.Unknown,
                TaskCanceledException => true,
                TimeoutException => true,
                HttpRequestException => true,
                _ => false
            };
        }

        private bool IsCircuitOpen(Type serviceType)
        {
            var failureCount = _failureCounts.GetValueOrDefault(serviceType, 0);
            return failureCount >= _options.CircuitBreakerThreshold;
        }

        private void RecordFailure(Type serviceType)
        {
            _failureCounts.AddOrUpdate(serviceType, 1, (key, value) => value + 1);
            _lastFailureTime[serviceType] = DateTime.UtcNow;
        }

        private void ResetCircuitBreaker(Type serviceType)
        {
            _failureCounts.TryRemove(serviceType, out _);
            _lastFailureTime.TryRemove(serviceType, out _);
            _logger.LogInformation("Circuit breaker reset for {ServiceType}", serviceType.Name);
        }

        #endregion
    }

    public enum CircuitBreakerState
    {
        Closed,
        Open,
        HalfOpen
    }
}

// =====================================================================================
// 3. SIMPLIFIED CONFIGURATION ORCHESTRATOR
// =====================================================================================

namespace Instrument.Data.Orchestration
{
    using Instrument.Data.Execution.Contracts;
    using Instrument.Data.Services.Grpc;

    /// <summary>
    /// Simplified configuration orchestrator using DI-registered gRPC clients
    /// </summary>
    public interface IConfigurationOrchestrator
    {
        Task<ConfigurationImportResult> ImportCurrentConfigurationAsync(
            bool includeSequences = true,
            bool clearExistingData = false,
            CancellationToken cancellationToken = default);

        Task<ConfigurationImportResult> ImportSequenceConfigurationAsync(
            string sequenceKey,
            CancellationToken cancellationToken = default);

        Task<ConfigurationImportResult> ImportResourceConfigurationAsync(
            string resourceKey,
            CancellationToken cancellationToken = default);

        Task<bool> TestExecutionServiceConnectionAsync(CancellationToken cancellationToken = default);
    }

    public class ConfigurationOrchestrator : IConfigurationOrchestrator
    {
        private readonly IResilientGrpcService _grpcService;
        private readonly IDataInitializer _dataInitializer;
        private readonly ILogger<ConfigurationOrchestrator> _logger;
        private readonly ConfigurationOrchestratorOptions _options;

        // Domain services
        private readonly ISequenceService _sequenceService;
        private readonly IResourceService _resourceService;
        private readonly IParameterService _parameterService;

        public ConfigurationOrchestrator(
            IResilientGrpcService grpcService,
            IDataInitializer dataInitializer,
            ISequenceService sequenceService,
            IResourceService resourceService,
            IParameterService parameterService,
            ILogger<ConfigurationOrchestrator> logger,
            IOptions<ConfigurationOrchestratorOptions> options)
        {
            _grpcService = grpcService;
            _dataInitializer = dataInitializer;
            _sequenceService = sequenceService;
            _resourceService = resourceService;
            _parameterService = parameterService;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<ConfigurationImportResult> ImportCurrentConfigurationAsync(
            bool includeSequences = true,
            bool clearExistingData = false,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ConfigurationImportResult();

            try
            {
                _logger.LogInformation("Starting configuration import (IncludeSequences: {IncludeSequences}, ClearData: {ClearData})",
                    includeSequences, clearExistingData);

                // Step 1: Fetch configuration from execution service
                var request = new GetCurrentConfigurationRequest(includeSequences);
                
                var response = await _grpcService.ExecuteAsync<IExecutionConfigurationService, GetCurrentConfigurationResponse>(
                    async (client, ct) => await client.GetCurrentConfigurationAsync(request, ct),
                    "GetCurrentConfiguration",
                    cancellationToken);

                if (response?.Configuration == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Configuration is null in response";
                    return result;
                }

                result.RequestId = $"{response.RequestId.Lo}-{response.RequestId.Hi}";

                // Step 2: Initialize/Clear database if requested
                if (clearExistingData)
                {
                    await ClearExistingDataAsync(cancellationToken);
                }

                await _dataInitializer.InitializeAsync(cancellationToken);

                // Step 3: Import configuration data
                await ImportConfigurationDataAsync(response.Configuration, result, cancellationToken);

                result.Success = true;
                _logger.LogInformation("Configuration import completed successfully. " +
                    "Sequences: {SeqCount}, Resources: {ResCount}, Parameters: {ParamCount}",
                    result.SequencesImported, result.ResourcesImported, result.ParametersImported);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Configuration import failed");
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                result.ExecutionTime = stopwatch.Elapsed;
            }

            return result;
        }

        public async Task<ConfigurationImportResult> ImportSequenceConfigurationAsync(
            string sequenceKey,
            CancellationToken cancellationToken = default)
        {
            var result = new ConfigurationImportResult();

            try
            {
                _logger.LogInformation("Importing sequence configuration for key: {SequenceKey}", sequenceKey);

                var request = new GetSequenceConfigurationRequest(sequenceKey);
                
                var response = await _grpcService.ExecuteAsync<IExecutionConfigurationService, GetSequenceConfigurationResponse>(
                    async (client, ct) => await client.GetSequenceConfigurationAsync(request, ct),
                    $"GetSequenceConfiguration_{sequenceKey}",
                    cancellationToken);

                if (response?.Sequence == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Sequence not found or response is null";
                    return result;
                }

                await ImportSingleSequenceAsync(response.Sequence, cancellationToken);
                
                result.Success = true;
                result.SequencesImported = 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import sequence configuration for key: {SequenceKey}", sequenceKey);
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<ConfigurationImportResult> ImportResourceConfigurationAsync(
            string resourceKey,
            CancellationToken cancellationToken = default)
        {
            var result = new ConfigurationImportResult();

            try
            {
                _logger.LogInformation("Importing resource configuration for key: {ResourceKey}", resourceKey);

                var request = new GetResourceConfigurationRequest(resourceKey);
                
                var response = await _grpcService.ExecuteAsync<IExecutionConfigurationService, GetResourceConfigurationResponse>(
                    async (client, ct) => await client.GetResourceConfigurationAsync(request, ct),
                    $"GetResourceConfiguration_{resourceKey}",
                    cancellationToken);

                if (response?.Resource == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Resource not found or response is null";
                    return result;
                }

                await ImportSingleResourceAsync(response.Resource, cancellationToken);
                
                result.Success = true;
                result.ResourcesImported = 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import resource configuration for key: {ResourceKey}", resourceKey);
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<bool> TestExecutionServiceConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _grpcService.TestServiceAsync<IExecutionConfigurationService>(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to test execution service connection");
                return false;
            }
        }

        #region Private Helper Methods (Same as before)

        private async Task ClearExistingDataAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Clearing existing configuration data");
            
            // Clear in dependency order
            await _sequenceService.ClearAllSequencesAsync(cancellationToken);
            await _resourceService.ClearAllResourcesAsync(cancellationToken);
            await _parameterService.ClearAllParametersAsync(cancellationToken);

            _logger.LogInformation("Existing configuration data cleared");
        }

        private async Task ImportConfigurationDataAsync(
            ExecutionConfigurationContract config,
            ConfigurationImportResult result,
            CancellationToken cancellationToken)
        {
            foreach (var sequence in config.Sequences)
            {
                await ImportSingleSequenceAsync(sequence, cancellationToken);
                result.SequencesImported++;
                result.ResourcesImported += sequence.Resources.Count;
                result.ParametersImported += sequence.Parameters.Count;
            }
        }

        private async Task ImportSingleSequenceAsync(
            ExecutionSequenceContract sequenceContract,
            CancellationToken cancellationToken)
        {
            // Map to your domain entity
            var sequence = new Sequence
            {
                Name = sequenceContract.Key,
                // Map other properties based on your domain model
            };

            var existingSequence = await _sequenceService.GetSequenceByNameAsync(sequenceContract.Key, cancellationToken);
            if (existingSequence == null)
            {
                await _sequenceService.CreateSequenceAsync(sequence, cancellationToken);
            }
            else
            {
                await _sequenceService.UpdateSequenceAsync(sequence, cancellationToken);
            }

            // Import associated resources and parameters
            foreach (var resourceContract in sequenceContract.Resources)
            {
                await ImportSingleResourceAsync(resourceContract, cancellationToken);
            }

            foreach (var parameterContract in sequenceContract.Parameters)
            {
                await ImportSingleParameterAsync(parameterContract, sequenceContract.Key, cancellationToken);
            }
        }

        private async Task ImportSingleResourceAsync(ExecutionResourceContract resourceContract, CancellationToken cancellationToken)
        {
            var resource = new Resource
            {
                Name = resourceContract.Key,
                // Map other properties
            };

            var existingResource = await _resourceService.GetResourceByNameAsync(resourceContract.Key, cancellationToken);
            if (existingResource == null)
            {
                await _resourceService.CreateResourceAsync(resource, cancellationToken);
            }
            else
            {
                await _resourceService.UpdateResourceAsync(resource, cancellationToken);
            }
        }

        private async Task ImportSingleParameterAsync(
            SequenceParameterTypeContract parameterContract,
            string sequenceKey,
            CancellationToken cancellationToken)
        {
            var parameter = new Parameter
            {
                Name = parameterContract.ParameterName,
                ParameterType = MapParameterType(parameterContract.ParameterType),
            };

            var existingParameter = await _parameterService.GetParameterByNameAsync(parameterContract.ParameterName, cancellationToken);
            if (existingParameter == null)
            {
                await _parameterService.CreateParameterAsync(parameter, cancellationToken);
            }
            else
            {
                await _parameterService.UpdateParameterAsync(parameter, cancellationToken);
            }
        }

        private Entities.Enums.ParameterType MapParameterType(ParameterType grpcParameterType)
        {
            return grpcParameterType switch
            {
                ParameterType.StringType => Entities.Enums.ParameterType.String,
                ParameterType.IntegerType => Entities.Enums.ParameterType.Integer,
                ParameterType.DecimalType => Entities.Enums.ParameterType.Decimal,
                ParameterType.BooleanType => Entities.Enums.ParameterType.Boolean,
                ParameterType.ArrayType => Entities.Enums.ParameterType.Array,
                ParameterType.EnumType => Entities.Enums.ParameterType.Enum,
                _ => Entities.Enums.ParameterType.String
            };
        }

        #endregion
    }

    // Configuration options and result classes remain the same...
    public class ConfigurationOrchestratorOptions
    {
        public const string SectionName = "ConfigurationOrchestrator";
        public bool ValidateDataBeforeInsert { get; set; } = true;
        public bool InitializeDatabaseOnStartup { get; set; } = true;
        public int MaxRetryAttempts { get; set; } = 3;
        public TimeSpan ImportTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }

    public class ConfigurationImportResult
    {
        public bool Success { get; set; }
        public int SequencesImported { get; set; }
        public int ResourcesImported { get; set; }
        public int ParametersImported { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public bool DataFromCache { get; set; }
        public string RequestId { get; set; }
    }
}

// =====================================================================================
// 4. DEPENDENCY INJECTION SETUP
// =====================================================================================

namespace Instrument.Data.Extensions
{
    public static class ConfigurationServicesExtensions
    {
        /// <summary>
        /// Register configuration services using DI-based gRPC clients
        /// </summary>
        public static IServiceCollection AddConfigurationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register options
            services.Configure<ResilientGrpcOptions>(
                configuration.GetSection(ResilientGrpcOptions.SectionName));
            services.Configure<ConfigurationOrchestratorOptions>(
                configuration.GetSection(ConfigurationOrchestratorOptions.SectionName));

            // Register resilient gRPC service
            services.AddMemoryCache();
            services.AddSingleton<IResilientGrpcService, ResilientGrpcService>();

            // Register orchestrator
            services.AddScoped<IConfigurationOrchestrator, ConfigurationOrchestrator>();
            services.AddScoped<IStartupConfigurationService, StartupConfigurationService>();

            // Register your gRPC client (this is where you'd use your existing DI setup)
            // Example:
            // services.AddCodeFirstGrpcClient<IExecutionConfigurationService>(options =>
            // {
            //     options.Address = new Uri(configuration["ExecutionService:BaseAddress"]);
            // });

            return services;
        }
    }
}

// =====================================================================================
// 5. CONFIGURATION EXAMPLE (appsettings.json)
// =====================================================================================

/*
{
  "ResilientGrpc": {
    "EnableCaching": true,
    "CacheExpiryMinutes": 15,
    "MaxRetryAttempts": 3,
    "RetryDelayMs": 1000,
    "RetryBackoffMultiplier": 2.0,
    "CircuitBreakerThreshold": 5,
    "CircuitBreakerTimeoutSeconds": 30,
    "TimeoutSeconds": 30
  },
  "ConfigurationOrchestrator": {
    "ValidateDataBeforeInsert": true,
    "InitializeDatabaseOnStartup": true,
    "MaxRetryAttempts": 3,
    "ImportTimeout": "00:05:00"
  },
  "ExecutionService": {
    "BaseAddress": "https://execution-service:5001"
  }
}
*/

// =====================================================================================
// 6. EXAMPLE DI REGISTRATION IN PROGRAM.CS
// =====================================================================================

/*
// In Program.cs

// Register your existing gRPC client
services.AddCodeFirstGrpcClient<IExecutionConfigurationService>(options =>
{
    options.Address = new Uri(configuration["ExecutionService:BaseAddress"]);
});

// Register configuration services (replaces the complex gateway)
services.AddConfigurationServices(configuration);

// Register your existing data services
services.AddSchedulerDataServices(configuration);

// Usage in startup
public static async Task Main(string[] args)
{
    var host = CreateHostBuilder(args).Build();
    
    using (var scope = host.Services.CreateScope())
    {
        var startupService = scope.ServiceProvider.GetRequiredService<IStartupConfigurationService>();
        await startupService.ImportConfigurationOnStartupAsync();
    }
    
    await host.RunAsync();
}
*/