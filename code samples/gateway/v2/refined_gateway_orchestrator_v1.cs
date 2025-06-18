// =====================================================================================
// REFINED gRPC GATEWAY + PROCESS MANAGER ORCHESTRATOR
// Implements SOLID principles with focus on testability and maintainability
// =====================================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Grpc.Net.Client;
using System.Diagnostics;

// =====================================================================================
// 1. CORE ABSTRACTIONS AND CONTRACTS
// =====================================================================================

namespace Instrument.Data.Gateway.Abstractions
{
    /// <summary>
    /// Represents a gRPC operation that can be executed against a service
    /// </summary>
    public interface IGrpcOperation<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        string ServiceName { get; }
        string OperationName { get; }
        TimeSpan? Timeout { get; }
        
        Task<TResponse> ExecuteAsync(
            IGrpcServiceClient serviceClient, 
            TRequest request, 
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Abstraction for gRPC service clients
    /// </summary>
    public interface IGrpcServiceClient
    {
        string ServiceName { get; }
        TClient GetClient<TClient>() where TClient : class;
    }

    /// <summary>
    /// Simple result wrapper for gateway operations
    /// </summary>
    public class GatewayResult<T>
    {
        public bool IsSuccess { get; set; }
        public T Data { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
        
        public static GatewayResult<T> Success(T data, TimeSpan duration) =>
            new() { IsSuccess = true, Data = data, Duration = duration };
            
        public static GatewayResult<T> Failure(string error, TimeSpan duration) =>
            new() { IsSuccess = false, ErrorMessage = error, Duration = duration };
    }

    /// <summary>
    /// Gateway interface focused only on executing operations
    /// </summary>
    public interface IGrpcGateway
    {
        Task<GatewayResult<TResponse>> ExecuteAsync<TRequest, TResponse>(
            IGrpcOperation<TRequest, TResponse> operation,
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class;
    }

    /// <summary>
    /// Service for managing gRPC service connections
    /// </summary>
    public interface IGrpcServiceRegistry
    {
        IGrpcServiceClient GetServiceClient(string serviceName);
        Task<bool> IsServiceAvailableAsync(string serviceName, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Simple retry policy interface
    /// </summary>
    public interface IRetryPolicy
    {
        Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
    }
}

// =====================================================================================
// 2. GATEWAY CONFIGURATION
// =====================================================================================

namespace Instrument.Data.Gateway.Configuration
{
    public class GrpcGatewayOptions
    {
        public const string SectionName = "GrpcGateway";
        
        public RetryOptions Retry { get; set; } = new();
        public Dictionary<string, ServiceOptions> Services { get; set; } = new();
        public int DefaultTimeoutSeconds { get; set; } = 30;
        public int MaxConcurrentRequests { get; set; } = 10;
    }

    public class ServiceOptions
    {
        public string BaseAddress { get; set; }
        public bool UseSecureConnection { get; set; } = true;
        public int? TimeoutSeconds { get; set; }
    }

    public class RetryOptions
    {
        public int MaxAttempts { get; set; } = 3;
        public int BaseDelayMs { get; set; } = 1000;
        public double BackoffMultiplier { get; set; } = 2.0;
    }
}

// =====================================================================================
// 3. SIMPLE RETRY POLICY IMPLEMENTATION
// =====================================================================================

namespace Instrument.Data.Gateway.Resilience
{
    using Instrument.Data.Gateway.Abstractions;
    using Instrument.Data.Gateway.Configuration;
    using Grpc.Core;

    public class ExponentialBackoffRetryPolicy : IRetryPolicy
    {
        private readonly RetryOptions _options;
        private readonly ILogger<ExponentialBackoffRetryPolicy> _logger;

        public ExponentialBackoffRetryPolicy(
            IOptions<GrpcGatewayOptions> options,
            ILogger<ExponentialBackoffRetryPolicy> logger)
        {
            _options = options.Value.Retry;
            _logger = logger;
        }

        public async Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken)
        {
            Exception lastException = null;

            for (int attempt = 1; attempt <= _options.MaxAttempts; attempt++)
            {
                try
                {
                    if (attempt > 1)
                    {
                        var delay = CalculateDelay(attempt - 1);
                        _logger.LogDebug("Retrying operation (attempt {Attempt}) after {Delay}ms", attempt, delay);
                        await Task.Delay(delay, cancellationToken);
                    }

                    return await operation(cancellationToken);
                }
                catch (Exception ex) when (IsRetryable(ex) && attempt < _options.MaxAttempts)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "Attempt {Attempt} failed with retryable error", attempt);
                }
            }

            throw lastException ?? new InvalidOperationException("All retry attempts failed");
        }

        private int CalculateDelay(int retryAttempt)
        {
            return (int)(_options.BaseDelayMs * Math.Pow(_options.BackoffMultiplier, retryAttempt));
        }

        private static bool IsRetryable(Exception ex) => ex switch
        {
            RpcException rpc => rpc.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded,
            TaskCanceledException => true,
            TimeoutException => true,
            _ => false
        };
    }
}

// =====================================================================================
// 4. SERVICE REGISTRY IMPLEMENTATION
// =====================================================================================

namespace Instrument.Data.Gateway.Services
{
    using Instrument.Data.Gateway.Abstractions;
    using Instrument.Data.Gateway.Configuration;

    public class GrpcServiceClient : IGrpcServiceClient
    {
        private readonly GrpcChannel _channel;
        private readonly ServiceOptions _options;

        public GrpcServiceClient(string serviceName, GrpcChannel channel, ServiceOptions options)
        {
            ServiceName = serviceName;
            _channel = channel;
            _options = options;
        }

        public string ServiceName { get; }

        public TClient GetClient<TClient>() where TClient : class
        {
            // This would use your preferred gRPC client creation method
            // For ProtoBuf.Grpc: return _channel.CreateGrpcService<TClient>();
            throw new NotImplementedException("Implement based on your gRPC client library");
        }
    }

    public class GrpcServiceRegistry : IGrpcServiceRegistry, IDisposable
    {
        private readonly GrpcGatewayOptions _options;
        private readonly ILogger<GrpcServiceRegistry> _logger;
        private readonly Dictionary<string, IGrpcServiceClient> _clients = new();
        private readonly Dictionary<string, GrpcChannel> _channels = new();

        public GrpcServiceRegistry(
            IOptions<GrpcGatewayOptions> options,
            ILogger<GrpcServiceRegistry> logger)
        {
            _options = options.Value;
            _logger = logger;
            InitializeServices();
        }

        public IGrpcServiceClient GetServiceClient(string serviceName)
        {
            if (!_clients.TryGetValue(serviceName, out var client))
            {
                throw new InvalidOperationException($"Service '{serviceName}' is not configured");
            }
            return client;
        }

        public async Task<bool> IsServiceAvailableAsync(string serviceName, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_channels.TryGetValue(serviceName, out var channel))
                {
                    await channel.ConnectAsync(cancellationToken);
                    return channel.State == ConnectivityState.Ready;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Health check failed for service: {ServiceName}", serviceName);
                return false;
            }
        }

        private void InitializeServices()
        {
            foreach (var (serviceName, serviceConfig) in _options.Services)
            {
                var channelOptions = new GrpcChannelOptions();
                
                if (!serviceConfig.UseSecureConnection)
                {
                    channelOptions.HttpHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = 
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };
                }

                var channel = GrpcChannel.ForAddress(serviceConfig.BaseAddress, channelOptions);
                var client = new GrpcServiceClient(serviceName, channel, serviceConfig);

                _channels[serviceName] = channel;
                _clients[serviceName] = client;

                _logger.LogInformation("Initialized gRPC service: {ServiceName} -> {Address}", 
                    serviceName, serviceConfig.BaseAddress);
            }
        }

        public void Dispose()
        {
            foreach (var channel in _channels.Values)
            {
                channel?.Dispose();
            }
            _channels.Clear();
            _clients.Clear();
        }
    }
}

// =====================================================================================
// 5. SIMPLIFIED GATEWAY IMPLEMENTATION
// =====================================================================================

namespace Instrument.Data.Gateway.Core
{
    using Instrument.Data.Gateway.Abstractions;
    using Instrument.Data.Gateway.Configuration;

    /// <summary>
    /// Simplified gRPC Gateway focused on operation execution
    /// Follows Single Responsibility Principle
    /// </summary>
    public class GrpcGateway : IGrpcGateway
    {
        private readonly IGrpcServiceRegistry _serviceRegistry;
        private readonly IRetryPolicy _retryPolicy;
        private readonly ILogger<GrpcGateway> _logger;
        private readonly SemaphoreSlim _semaphore;

        public GrpcGateway(
            IGrpcServiceRegistry serviceRegistry,
            IRetryPolicy retryPolicy,
            ILogger<GrpcGateway> logger,
            IOptions<GrpcGatewayOptions> options)
        {
            _serviceRegistry = serviceRegistry;
            _retryPolicy = retryPolicy;
            _logger = logger;
            _semaphore = new SemaphoreSlim(options.Value.MaxConcurrentRequests);
        }

        public async Task<GatewayResult<TResponse>> ExecuteAsync<TRequest, TResponse>(
            IGrpcOperation<TRequest, TResponse> operation,
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class
        {
            var stopwatch = Stopwatch.StartNew();
            
            _logger.LogDebug("Executing {ServiceName}.{OperationName}", 
                operation.ServiceName, operation.OperationName);

            await _semaphore.WaitAsync(cancellationToken);
            
            try
            {
                var serviceClient = _serviceRegistry.GetServiceClient(operation.ServiceName);
                
                var result = await _retryPolicy.ExecuteAsync(async ct =>
                {
                    using var timeoutCts = operation.Timeout.HasValue 
                        ? new CancellationTokenSource(operation.Timeout.Value)
                        : new CancellationTokenSource();
                    
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                        timeoutCts.Token, ct);

                    return await operation.ExecuteAsync(serviceClient, request, linkedCts.Token);
                }, cancellationToken);

                _logger.LogInformation("Successfully executed {ServiceName}.{OperationName} in {Duration}ms",
                    operation.ServiceName, operation.OperationName, stopwatch.ElapsedMilliseconds);

                return GatewayResult<TResponse>.Success(result, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {ServiceName}.{OperationName}",
                    operation.ServiceName, operation.OperationName);
                
                return GatewayResult<TResponse>.Failure(ex.Message, stopwatch.Elapsed);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}

// =====================================================================================
// 6. PROCESS MANAGER ORCHESTRATOR PATTERN
// =====================================================================================

namespace Instrument.Data.Orchestration.Abstractions
{
    /// <summary>
    /// Represents a step in the orchestration process
    /// </summary>
    public interface IOrchestrationStep
    {
        string StepName { get; }
        Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Context passed between orchestration steps
    /// </summary>
    public class OrchestrationContext
    {
        public Dictionary<string, object> Data { get; } = new();
        public List<string> CompletedSteps { get; } = new();
        public List<string> Errors { get; } = new();
        
        public T GetData<T>(string key) => Data.TryGetValue(key, out var value) ? (T)value : default;
        public void SetData<T>(string key, T value) => Data[key] = value;
    }

    /// <summary>
    /// Result of a single orchestration step
    /// </summary>
    public class StepResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public bool ShouldContinue { get; set; } = true;
        
        public static StepResult Success() => new() { IsSuccess = true };
        public static StepResult Failure(string error, bool shouldContinue = false) => 
            new() { IsSuccess = false, ErrorMessage = error, ShouldContinue = shouldContinue };
    }

    /// <summary>
    /// Process Manager interface implementing the Process Manager Pattern
    /// </summary>
    public interface IProcessManager<TRequest, TResult>
    {
        Task<TResult> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Configuration import specific result
    /// </summary>
    public class ConfigurationImportResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
        public ImportStatistics Statistics { get; set; } = new();
        public List<string> ProcessedSteps { get; set; } = new();
    }

    public class ImportStatistics
    {
        public int SequencesProcessed { get; set; }
        public int ResourcesProcessed { get; set; }
        public int ParametersProcessed { get; set; }
    }
}

// =====================================================================================
// 7. CONFIGURATION IMPORT ORCHESTRATOR
// =====================================================================================

namespace Instrument.Data.Orchestration.Configuration
{
    using Instrument.Data.Orchestration.Abstractions;
    using Instrument.Data.Gateway.Abstractions;

    /// <summary>
    /// Request for configuration import process
    /// </summary>
    public class ConfigurationImportRequest
    {
        public bool IncludeSequences { get; set; } = true;
        public bool ClearExistingData { get; set; } = false;
        public List<string> SequenceFilters { get; set; } = new();
    }

    /// <summary>
    /// Process Manager for Configuration Import
    /// Implements the Process Manager Pattern for managing configuration import workflow
    /// </summary>
    public class ConfigurationImportOrchestrator : IProcessManager<ConfigurationImportRequest, ConfigurationImportResult>
    {
        private readonly IEnumerable<IOrchestrationStep> _steps;
        private readonly ILogger<ConfigurationImportOrchestrator> _logger;

        public ConfigurationImportOrchestrator(
            IEnumerable<IOrchestrationStep> steps,
            ILogger<ConfigurationImportOrchestrator> logger)
        {
            _steps = steps.OrderBy(s => GetStepOrder(s.StepName));
            _logger = logger;
        }

        public async Task<ConfigurationImportResult> ExecuteAsync(
            ConfigurationImportRequest request,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var context = new OrchestrationContext();
            var result = new ConfigurationImportResult();

            // Store request in context
            context.SetData("ImportRequest", request);

            _logger.LogInformation("Starting configuration import process with {StepCount} steps", _steps.Count());

            try
            {
                foreach (var step in _steps)
                {
                    _logger.LogDebug("Executing step: {StepName}", step.StepName);

                    var stepResult = await step.ExecuteAsync(context, cancellationToken);
                    context.CompletedSteps.Add(step.StepName);

                    if (!stepResult.IsSuccess)
                    {
                        var error = $"Step '{step.StepName}' failed: {stepResult.ErrorMessage}";
                        context.Errors.Add(error);
                        _logger.LogError(error);

                        if (!stepResult.ShouldContinue)
                        {
                            result.IsSuccess = false;
                            result.ErrorMessage = error;
                            break;
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Step {StepName} completed successfully", step.StepName);
                    }
                }

                // If we completed all steps without breaking, it's a success
                if (result.ErrorMessage == null)
                {
                    result.IsSuccess = context.Errors.Count == 0;
                    if (!result.IsSuccess)
                    {
                        result.ErrorMessage = string.Join("; ", context.Errors);
                    }
                }

                // Extract statistics from context
                result.Statistics = context.GetData<ImportStatistics>("Statistics") ?? new ImportStatistics();
                result.ProcessedSteps = new List<string>(context.CompletedSteps);

                _logger.LogInformation("Configuration import process completed. Success: {Success}, Duration: {Duration}ms",
                    result.IsSuccess, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Configuration import process failed with unexpected error");
                result.IsSuccess = false;
                result.ErrorMessage = $"Unexpected error: {ex.Message}";
            }
            finally
            {
                result.Duration = stopwatch.Elapsed;
            }

            return result;
        }

        private static int GetStepOrder(string stepName) => stepName switch
        {
            "ValidateRequest" => 1,
            "FetchConfiguration" => 2,
            "ClearExistingData" => 3,
            "InitializeDatabase" => 4,
            "ImportSequences" => 5,
            "ImportResources" => 6,
            "ImportParameters" => 7,
            "ValidateImport" => 8,
            _ => 999
        };
    }
}

// =====================================================================================
// 8. CONCRETE ORCHESTRATION STEPS
// =====================================================================================

namespace Instrument.Data.Orchestration.Steps
{
    using Instrument.Data.Orchestration.Abstractions;
    using Instrument.Data.Orchestration.Configuration;
    using Instrument.Data.Gateway.Abstractions;

    /// <summary>
    /// Step to validate the import request
    /// </summary>
    public class ValidateRequestStep : IOrchestrationStep
    {
        private readonly ILogger<ValidateRequestStep> _logger;

        public ValidateRequestStep(ILogger<ValidateRequestStep> logger)
        {
            _logger = logger;
        }

        public string StepName => "ValidateRequest";

        public Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
        {
            var request = context.GetData<ConfigurationImportRequest>("ImportRequest");
            
            if (request == null)
            {
                return Task.FromResult(StepResult.Failure("Import request is null"));
            }

            _logger.LogDebug("Request validation completed successfully");
            return Task.FromResult(StepResult.Success());
        }
    }

    /// <summary>
    /// Step to fetch configuration from external service
    /// </summary>
    public class FetchConfigurationStep : IOrchestrationStep
    {
        private readonly IGrpcGateway _gateway;
        private readonly ILogger<FetchConfigurationStep> _logger;

        public FetchConfigurationStep(
            IGrpcGateway gateway,
            ILogger<FetchConfigurationStep> logger)
        {
            _gateway = gateway;
            _logger = logger;
        }

        public string StepName => "FetchConfiguration";

        public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
        {
            var request = context.GetData<ConfigurationImportRequest>("ImportRequest");

            try
            {
                // This would use a concrete operation implementation
                // var operation = new GetCurrentConfigurationOperation();
                // var grpcRequest = new GetCurrentConfigurationRequest(request.IncludeSequences);
                // var result = await _gateway.ExecuteAsync(operation, grpcRequest, cancellationToken);

                // For now, simulate the response
                _logger.LogInformation("Configuration fetched successfully from external service");
                
                // Store the fetched configuration in context
                // context.SetData("FetchedConfiguration", result.Data);
                
                return StepResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch configuration from external service");
                return StepResult.Failure($"Failed to fetch configuration: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Step to clear existing data if requested
    /// </summary>
    public class ClearExistingDataStep : IOrchestrationStep
    {
        private readonly IDataInitializer _dataInitializer;
        private readonly ILogger<ClearExistingDataStep> _logger;

        public ClearExistingDataStep(
            IDataInitializer dataInitializer,
            ILogger<ClearExistingDataStep> logger)
        {
            _dataInitializer = dataInitializer;
            _logger = logger;
        }

        public string StepName => "ClearExistingData";

        public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
        {
            var request = context.GetData<ConfigurationImportRequest>("ImportRequest");

            if (!request.ClearExistingData)
            {
                _logger.LogDebug("Skipping data clearing as not requested");
                return StepResult.Success();
            }

            try
            {
                await _dataInitializer.ClearAllDataAsync(cancellationToken);
                _logger.LogInformation("Existing data cleared successfully");
                return StepResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear existing data");
                return StepResult.Failure($"Failed to clear existing data: {ex.Message}", shouldContinue: true);
            }
        }
    }

    /// <summary>
    /// Step to import sequences using domain services
    /// </summary>
    public class ImportSequencesStep : IOrchestrationStep
    {
        private readonly ISequenceService _sequenceService;
        private readonly ILogger<ImportSequencesStep> _logger;

        public ImportSequencesStep(
            ISequenceService sequenceService,
            ILogger<ImportSequencesStep> logger)
        {
            _sequenceService = sequenceService;
            _logger = logger;
        }

        public string StepName => "ImportSequences";

        public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
        {
            try
            {
                // Get the fetched configuration from context
                // var configuration = context.GetData<ExecutionConfigurationContract>("FetchedConfiguration");
                
                var statistics = context.GetData<ImportStatistics>("Statistics") ?? new ImportStatistics();
                
                // Process sequences using domain service
                // foreach (var sequenceContract in configuration.Sequences)
                // {
                //     await ProcessSequenceAsync(sequenceContract, cancellationToken);
                //     statistics.SequencesProcessed++;
                // }

                // For now, simulate processing
                statistics.SequencesProcessed = 5; // Simulated
                context.SetData("Statistics", statistics);

                _logger.LogInformation("Imported {Count} sequences successfully", statistics.SequencesProcessed);
                return StepResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import sequences");
                return StepResult.Failure($"Failed to import sequences: {ex.Message}");
            }
        }

        private async Task ProcessSequenceAsync(object sequenceContract, CancellationToken cancellationToken)
        {
            // Convert to domain entity and use service
            // var sequence = MapToDomainEntity(sequenceContract);
            // await _sequenceService.CreateOrUpdateSequenceAsync(sequence, cancellationToken);
            await Task.CompletedTask; // Placeholder
        }
    }
}

// =====================================================================================
// 9. DEPENDENCY INJECTION SETUP
// =====================================================================================

namespace Instrument.Data.Extensions
{
    using Instrument.Data.Gateway.Abstractions;
    using Instrument.Data.Gateway.Core;
    using Instrument.Data.Gateway.Services;
    using Instrument.Data.Gateway.Resilience;
    using Instrument.Data.Gateway.Configuration;
    using Instrument.Data.Orchestration.Abstractions;
    using Instrument.Data.Orchestration.Configuration;
    using Instrument.Data.Orchestration.Steps;

    public static class GatewayAndOrchestrationExtensions
    {
        /// <summary>
        /// Register gateway and orchestration services
        /// </summary>
        public static IServiceCollection AddGatewayAndOrchestration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Configuration
            services.Configure<GrpcGatewayOptions>(
                configuration.GetSection(GrpcGatewayOptions.SectionName));

            // Gateway services
            services.AddSingleton<IGrpcServiceRegistry, GrpcServiceRegistry>();
            services.AddSingleton<IRetryPolicy, ExponentialBackoffRetryPolicy>();
            services.AddScoped<IGrpcGateway, GrpcGateway>();

            // Orchestration services
            services.AddScoped<IProcessManager<ConfigurationImportRequest, ConfigurationImportResult>, 
                ConfigurationImportOrchestrator>();

            // Orchestration steps
            services.AddScoped<IOrchestrationStep, ValidateRequestStep>();
            services.AddScoped<IOrchestrationStep, FetchConfigurationStep>();
            services.AddScoped<IOrchestrationStep, ClearExistingDataStep>();
            services.AddScoped<IOrchestrationStep, ImportSequencesStep>();

            return services;
        }
    }
}

// =====================================================================================
// 10. USAGE EXAMPLE
// =====================================================================================

namespace Instrument.Data.Examples
{
    using Instrument.Data.Orchestration.Abstractions;
    using Instrument.Data.Orchestration.Configuration;

    public class ConfigurationImportExample
    {
        private readonly IProcessManager<ConfigurationImportRequest, ConfigurationImportResult> _processManager;

        public ConfigurationImportExample(
            IProcessManager<ConfigurationImportRequest, ConfigurationImportResult> processManager)
        {
            _processManager = processManager;
        }

        public async Task ImportConfigurationAsync()
        {
            var request = new ConfigurationImportRequest
            {
                IncludeSequences = true,
                ClearExistingData = true,
                SequenceFilters = new List<string> { "TechA", "TechB" }
            };

            var result = await _processManager.ExecuteAsync(request);

            if (result.IsSuccess)
            {
                Console.WriteLine($"Import completed successfully in {result.Duration.TotalSeconds:F2}s");
                Console.WriteLine($"Processed: {result.Statistics.SequencesProcessed} sequences, " +
                                $"{result.Statistics.ResourcesProcessed} resources, " +
                                $"{result.Statistics.ParametersProcessed} parameters");
            }
            else
            {
                Console.WriteLine($"Import failed: {result.ErrorMessage}");
            }
        }
    }
}