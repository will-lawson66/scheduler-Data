// =====================================================================================
// REFINED gRPC GATEWAY + PROCESS MANAGER ORCHESTRATOR
// Integrates with existing scheduler-Data services and DI-managed gRPC clients
// =====================================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;

// =====================================================================================
// 1. EXECUTION SERVICE gRPC CONTRACTS (FROM YOUR REQUIREMENTS)
// =====================================================================================

namespace Instrument.Data.ExecutionService.Contracts
{
    /// <summary>
    /// gRPC service interface for ExecutionConfigurationService
    /// Represents the remote service you want to connect to
    /// </summary>
    public interface IExecutionConfigurationService
    {
        Task<GetCurrentConfigurationResponse> GetCurrentConfigurationAsync(
            GetCurrentConfigurationRequest request, 
            CancellationToken cancellationToken = default);

        Task<GetSequenceConfigurationResponse> GetSequenceConfigurationAsync(
            GetSequenceConfigurationRequest request, 
            CancellationToken cancellationToken = default);

        Task<GetResourceConfigurationResponse> GetResourceConfigurationAsync(
            GetResourceConfigurationRequest request, 
            CancellationToken cancellationToken = default);
    }

    // Request/Response DTOs for ExecutionConfigurationService
    public record GetCurrentConfigurationRequest(bool IncludeSequences = true);
    public record GetSequenceConfigurationRequest(string Key);
    public record GetResourceConfigurationRequest(string Key);

    public record GetCurrentConfigurationResponse(
        GuidRequestId RequestId,
        ExecutionConfigurationContract? Configuration,
        IReadOnlyCollection<GrpcErrorContract> Errors
    );

    public record GetSequenceConfigurationResponse(
        GuidRequestId RequestId,
        ExecutionSequenceContract? Sequence,
        IReadOnlyCollection<GrpcErrorContract> Errors
    );

    public record GetResourceConfigurationResponse(
        GuidRequestId RequestId,
        ExecutionResourceContract? Resource,
        IReadOnlyCollection<GrpcErrorContract> Errors
    );

    // Configuration DTOs from ExecutionService
    public record ExecutionConfigurationContract(
        int StartingPeriod,
        int RolloverPeriod,
        TimeSpan PeriodSpan,
        double PeriodAcceleration,
        IReadOnlyCollection<ExecutionSequenceContract> Sequences
    );

    public record ExecutionSequenceContract(
        string Key,
        TimeSpan WorstCaseTime,
        string ExecutionMethod,
        string ScriptKey,
        IReadOnlyCollection<ExecutionResourceContract> Resources,
        IReadOnlyCollection<SequenceParameterTypeContract> Parameters
    );

    public record ExecutionResourceContract(
        string Key,
        bool HasScriptingInterface,
        string ScriptingInterface
    );

    public record SequenceParameterTypeContract(
        string ParameterName,
        ParameterType ParameterType
    );

    public record GuidRequestId(string Lo, string Hi);
    public record GrpcErrorContract(string Message, string Code);
}

// =====================================================================================
// 2. GATEWAY ABSTRACTIONS - SIMPLIFIED FOR MULTI-SERVICE ROUTING
// =====================================================================================

namespace Instrument.Data.Gateway.Abstractions
{
    /// <summary>
    /// Represents a gRPC operation that can be executed against a specific service
    /// </summary>
    public interface IGrpcOperation<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        string ServiceName { get; }
        string OperationName { get; }
        TimeSpan? Timeout { get; }
        
        Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken);
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
    /// Multi-service gateway interface
    /// </summary>
    public interface IGrpcGateway
    {
        Task<GatewayResult<TResponse>> ExecuteAsync<TRequest, TResponse>(
            IGrpcOperation<TRequest, TResponse> operation,
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class;

        Task<bool> IsServiceAvailableAsync(string serviceName, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Service registry for resolving DI-managed gRPC clients
    /// </summary>
    public interface IGrpcServiceRegistry
    {
        TService GetService<TService>() where TService : class;
        bool IsServiceRegistered<TService>() where TService : class;
    }
}

// =====================================================================================
// 3. GATEWAY CONFIGURATION
// =====================================================================================

namespace Instrument.Data.Gateway.Configuration
{
    public class GrpcGatewayOptions
    {
        public const string SectionName = "GrpcGateway";
        
        public int DefaultTimeoutSeconds { get; set; } = 30;
        public int MaxConcurrentRequests { get; set; } = 10;
        public RetryOptions Retry { get; set; } = new();
        
        // Service-specific timeout overrides
        public Dictionary<string, ServiceOptions> Services { get; set; } = new();
    }

    public class ServiceOptions
    {
        public int? TimeoutSeconds { get; set; }
        public string? BaseAddress { get; set; } // For documentation/health checks
    }

    public class RetryOptions
    {
        public int MaxAttempts { get; set; } = 3;
        public int BaseDelayMs { get; set; } = 1000;
        public double BackoffMultiplier { get; set; } = 2.0;
    }
}

// =====================================================================================
// 4. SIMPLE RETRY POLICY
// =====================================================================================

namespace Instrument.Data.Gateway.Resilience
{
    using Instrument.Data.Gateway.Configuration;
    using Grpc.Core;

    public interface IRetryPolicy
    {
        Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
    }

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
// 5. GATEWAY IMPLEMENTATION USING DI-MANAGED CLIENTS
// =====================================================================================

namespace Instrument.Data.Gateway.Core
{
    using Instrument.Data.Gateway.Abstractions;
    using Instrument.Data.Gateway.Configuration;
    using Instrument.Data.Gateway.Resilience;

    /// <summary>
    /// Service registry implementation using DI container
    /// </summary>
    public class DiGrpcServiceRegistry : IGrpcServiceRegistry
    {
        private readonly IServiceProvider _serviceProvider;

        public DiGrpcServiceRegistry(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public TService GetService<TService>() where TService : class
        {
            return _serviceProvider.GetRequiredService<TService>();
        }

        public bool IsServiceRegistered<TService>() where TService : class
        {
            return _serviceProvider.GetService<TService>() != null;
        }
    }

    /// <summary>
    /// Multi-service gRPC Gateway using DI-managed clients
    /// </summary>
    public class GrpcGateway : IGrpcGateway
    {
        private readonly IGrpcServiceRegistry _serviceRegistry;
        private readonly IRetryPolicy _retryPolicy;
        private readonly ILogger<GrpcGateway> _logger;
        private readonly GrpcGatewayOptions _options;
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
            _options = options.Value;
            _semaphore = new SemaphoreSlim(_options.MaxConcurrentRequests);
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
                var result = await _retryPolicy.ExecuteAsync(async ct =>
                {
                    // Apply timeout - check service-specific first, then default
                    var timeout = GetTimeoutForService(operation.ServiceName, operation.Timeout);
                    using var timeoutCts = new CancellationTokenSource(timeout);
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, ct);

                    return await operation.ExecuteAsync(request, linkedCts.Token);
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

        public async Task<bool> IsServiceAvailableAsync(string serviceName, CancellationToken cancellationToken = default)
        {
            try
            {
                // For DI-managed services, we check if they're registered
                // You could extend this to do actual health checks
                return serviceName switch
                {
                    "ExecutionConfigurationService" => _serviceRegistry.IsServiceRegistered<ExecutionService.Contracts.IExecutionConfigurationService>(),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Service availability check failed for: {ServiceName}", serviceName);
                return false;
            }
        }

        private TimeSpan GetTimeoutForService(string serviceName, TimeSpan? operationTimeout)
        {
            // Priority: operation timeout > service-specific timeout > default timeout
            if (operationTimeout.HasValue)
                return operationTimeout.Value;

            if (_options.Services.TryGetValue(serviceName, out var serviceOptions) 
                && serviceOptions.TimeoutSeconds.HasValue)
                return TimeSpan.FromSeconds(serviceOptions.TimeoutSeconds.Value);

            return TimeSpan.FromSeconds(_options.DefaultTimeoutSeconds);
        }
    }
}

// =====================================================================================
// 6. CONCRETE gRPC OPERATIONS FOR EXECUTION SERVICE
// =====================================================================================

namespace Instrument.Data.Gateway.Operations
{
    using Instrument.Data.Gateway.Abstractions;
    using Instrument.Data.ExecutionService.Contracts;

    /// <summary>
    /// Operation to get current configuration from ExecutionConfigurationService
    /// </summary>
    public class GetCurrentConfigurationOperation : IGrpcOperation<GetCurrentConfigurationRequest, GetCurrentConfigurationResponse>
    {
        private readonly IGrpcServiceRegistry _serviceRegistry;

        public GetCurrentConfigurationOperation(IGrpcServiceRegistry serviceRegistry)
        {
            _serviceRegistry = serviceRegistry;
        }

        public string ServiceName => "ExecutionConfigurationService";
        public string OperationName => "GetCurrentConfiguration";
        public TimeSpan? Timeout => TimeSpan.FromMinutes(2); // Large payload

        public async Task<GetCurrentConfigurationResponse> ExecuteAsync(
            GetCurrentConfigurationRequest request, 
            CancellationToken cancellationToken)
        {
            var client = _serviceRegistry.GetService<IExecutionConfigurationService>();
            return await client.GetCurrentConfigurationAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// Operation to get single sequence configuration
    /// </summary>
    public class GetSequenceConfigurationOperation : IGrpcOperation<GetSequenceConfigurationRequest, GetSequenceConfigurationResponse>
    {
        private readonly IGrpcServiceRegistry _serviceRegistry;

        public GetSequenceConfigurationOperation(IGrpcServiceRegistry serviceRegistry)
        {
            _serviceRegistry = serviceRegistry;
        }

        public string ServiceName => "ExecutionConfigurationService";
        public string OperationName => "GetSequenceConfiguration";
        public TimeSpan? Timeout => null; // Use default

        public async Task<GetSequenceConfigurationResponse> ExecuteAsync(
            GetSequenceConfigurationRequest request, 
            CancellationToken cancellationToken)
        {
            var client = _serviceRegistry.GetService<IExecutionConfigurationService>();
            return await client.GetSequenceConfigurationAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// Operation to get single resource configuration
    /// </summary>
    public class GetResourceConfigurationOperation : IGrpcOperation<GetResourceConfigurationRequest, GetResourceConfigurationResponse>
    {
        private readonly IGrpcServiceRegistry _serviceRegistry;

        public GetResourceConfigurationOperation(IGrpcServiceRegistry serviceRegistry)
        {
            _serviceRegistry = serviceRegistry;
        }

        public string ServiceName => "ExecutionConfigurationService";
        public string OperationName => "GetResourceConfiguration";
        public TimeSpan? Timeout => null; // Use default

        public async Task<GetResourceConfigurationResponse> ExecuteAsync(
            GetResourceConfigurationRequest request, 
            CancellationToken cancellationToken)
        {
            var client = _serviceRegistry.GetService<IExecutionConfigurationService>();
            return await client.GetResourceConfigurationAsync(request, cancellationToken);
        }
    }
}

// =====================================================================================
// 7. PROCESS MANAGER PATTERN FOR CONFIGURATION IMPORT
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
}

// =====================================================================================
// 8. CONFIGURATION IMPORT REQUEST/RESULT
// =====================================================================================

namespace Instrument.Data.Orchestration.Configuration
{
    using Instrument.Data.Orchestration.Abstractions;

    /// <summary>
    /// Request for configuration import process
    /// </summary>
    public class ConfigurationImportRequest
    {
        public bool IncludeSequences { get; set; } = true;
        public bool ClearExistingData { get; set; } = false;
        public List<string> SequenceFilters { get; set; } = new();
        public List<string> ResourceFilters { get; set; } = new();
    }

    /// <summary>
    /// Result of configuration import process
    /// </summary>
    public class ConfigurationImportResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
        public ImportStatistics Statistics { get; set; } = new();
        public List<string> ProcessedSteps { get; set; } = new();
        public string RequestId { get; set; }
    }

    public class ImportStatistics
    {
        public int SequencesProcessed { get; set; }
        public int ResourcesProcessed { get; set; }
        public int ParametersProcessed { get; set; }
        public int SequenceParameterLinksCreated { get; set; }
    }

    /// <summary>
    /// Configuration Import Process Manager
    /// Implements the Process Manager Pattern for orchestrating configuration import
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
            context.SetData("Statistics", new ImportStatistics());

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

                // If we completed all steps without breaking, check for any errors
                if (result.ErrorMessage == null)
                {
                    result.IsSuccess = context.Errors.Count == 0;
                    if (!result.IsSuccess)
                    {
                        result.ErrorMessage = string.Join("; ", context.Errors);
                    }
                }

                // Extract results from context
                result.Statistics = context.GetData<ImportStatistics>("Statistics") ?? new ImportStatistics();
                result.ProcessedSteps = new List<string>(context.CompletedSteps);
                result.RequestId = context.GetData<string>("RequestId") ?? Guid.NewGuid().ToString();

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
            "LinkSequenceParameters" => 8,
            "ValidateImport" => 9,
            _ => 999
        };
    }
}

// =====================================================================================
// 9. CONCRETE ORCHESTRATION STEPS FOR SCHEDULER-DATA
// =====================================================================================

namespace Instrument.Data.Orchestration.Steps
{
    using Instrument.Data.Orchestration.Abstractions;
    using Instrument.Data.Orchestration.Configuration;
    using Instrument.Data.Gateway.Abstractions;
    using Instrument.Data.Gateway.Operations;
    using Instrument.Data.ExecutionService.Contracts;

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

            _logger.LogDebug("Request validation completed successfully. IncludeSequences: {IncludeSequences}, ClearData: {ClearData}",
                request.IncludeSequences, request.ClearExistingData);
            
            return Task.FromResult(StepResult.Success());
        }
    }

    /// <summary>
    /// Step to fetch configuration from ExecutionConfigurationService
    /// </summary>
    public class FetchConfigurationStep : IOrchestrationStep
    {
        private readonly IGrpcGateway _gateway;
        private readonly IGrpcServiceRegistry _serviceRegistry;
        private readonly ILogger<FetchConfigurationStep> _logger;

        public FetchConfigurationStep(
            IGrpcGateway gateway,
            IGrpcServiceRegistry serviceRegistry,
            ILogger<FetchConfigurationStep> logger)
        {
            _gateway = gateway;
            _serviceRegistry = serviceRegistry;
            _logger = logger;
        }

        public string StepName => "FetchConfiguration";

        public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
        {
            var request = context.GetData<ConfigurationImportRequest>("ImportRequest");

            try
            {
                var operation = new GetCurrentConfigurationOperation(_serviceRegistry);
                var grpcRequest = new GetCurrentConfigurationRequest(request.IncludeSequences);
                
                var result = await _gateway.ExecuteAsync(operation, grpcRequest, cancellationToken);

                if (!result.IsSuccess)
                {
                    return StepResult.Failure($"Failed to fetch configuration: {result.ErrorMessage}");
                }

                if (result.Data?.Configuration == null)
                {
                    return StepResult.Failure("Configuration is null in response");
                }

                // Store the fetched configuration and metadata in context
                context.SetData("FetchedConfiguration", result.Data.Configuration);
                context.SetData("RequestId", $"{result.Data.RequestId.Lo}-{result.Data.RequestId.Hi}");
                
                _logger.LogInformation("Configuration fetched successfully. Sequences: {SequenceCount}", 
                    result.Data.Configuration.Sequences.Count);
                
                return StepResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch configuration from ExecutionConfigurationService");
                return StepResult.Failure($"Failed to fetch configuration: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Step to clear existing data if requested
    /// </summary>
    public class ClearExistingDataStep : IOrchestrationStep
    {
        private readonly ISequenceService _sequenceService;
        private readonly IResourceService _resourceService;
        private readonly IParameterService _parameterService;
        private readonly ILogger<ClearExistingDataStep> _logger;

        public ClearExistingDataStep(
            ISequenceService sequenceService,
            IResourceService resourceService,
            IParameterService parameterService,
            ILogger<ClearExistingDataStep> logger)
        {
            _sequenceService = sequenceService;
            _resourceService = resourceService;
            _parameterService = parameterService;
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
                // Clear data in dependency order (sequences first, then resources, then parameters)
                var existingSequences = await _sequenceService.GetAllSequencesAsync();
                foreach (var sequence in existingSequences)
                {
                    await _sequenceService.DeleteSequenceAsync(sequence.Id);
                }

                var existingResources = await _resourceService.GetAllResourcesAsync();
                foreach (var resource in existingResources)
                {
                    await _resourceService.DeleteResourceAsync(resource.Id);
                }

                var existingParameters = await _parameterService.GetAllParametersAsync();
                foreach (var parameter in existingParameters)
                {
                    await _parameterService.DeleteParameterAsync(parameter.Id);
                }

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
    /// Step to initialize database if needed
    /// </summary>
    public class InitializeDatabaseStep : IOrchestrationStep
    {
        private readonly IDataInitializer _dataInitializer;
        private readonly ILogger<InitializeDatabaseStep> _logger;

        public InitializeDatabaseStep(
            IDataInitializer dataInitializer,
            ILogger<InitializeDatabaseStep> logger)
        {
            _dataInitializer = dataInitializer;
            _logger = logger;
        }

        public string StepName => "InitializeDatabase";

        public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
        {
            try
            {
                await _dataInitializer.InitializeAsync();
                _logger.LogDebug("Database initialization completed");
                return StepResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database");
                return StepResult.Failure($"Failed to initialize database: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Step to import sequences from ExecutionService to scheduler-Data entities
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
                var configuration = context.GetData<ExecutionConfigurationContract>("FetchedConfiguration");
                var statistics = context.GetData<ImportStatistics>("Statistics");
                var request = context.GetData<ConfigurationImportRequest>("ImportRequest");

                if (configuration?.Sequences == null)
                {
                    return StepResult.Failure("No configuration or sequences found in context");
                }

                var sequencesToProcess = configuration.Sequences;

                // Apply filters if specified
                if (request.SequenceFilters?.Any() == true)
                {
                    sequencesToProcess = sequencesToProcess
                        .Where(s => request.SequenceFilters.Contains(s.Key))
                        .ToList();
                }

                foreach (var sequenceContract in sequencesToProcess)
                {
                    await ProcessSequenceAsync(sequenceContract, cancellationToken);
                    statistics.SequencesProcessed++;
                }

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

        private async Task ProcessSequenceAsync(ExecutionSequenceContract sequenceContract, CancellationToken cancellationToken)
        {
            // Map ExecutionSequenceContract to scheduler-Data Sequence entity
            var sequence = new Sequence
            {
                Name = sequenceContract.Key,
                WorstCaseTime = sequenceContract.WorstCaseTime,
                Description = $"Imported from ExecutionService - Method: {sequenceContract.ExecutionMethod}",
                CanBeParallel = false // Default value - could be enhanced based on ExecutionMethod
            };

            // Check if sequence already exists by name
            var existingSequences = await _sequenceService.GetAllSequencesAsync();
            var existingSequence = existingSequences.FirstOrDefault(s => s.Name == sequence.Name);

            if (existingSequence == null)
            {
                await _sequenceService.CreateSequenceAsync(sequence);
                _logger.LogDebug("Created new sequence: {SequenceName}", sequence.Name);
            }
            else
            {
                // Update existing sequence
                var updatedSequence = existingSequence.Update(
                    name: sequence.Name,
                    worstCaseTime: sequence.WorstCaseTime,
                    description: sequence.Description,
                    canBeParallel: sequence.CanBeParallel);
                
                await _sequenceService.UpdateSequenceAsync(updatedSequence);
                _logger.LogDebug("Updated existing sequence: {SequenceName}", sequence.Name);
            }
        }
    }

    /// <summary>
    /// Step to import resources from ExecutionService to scheduler-Data entities
    /// </summary>
    public class ImportResourcesStep : IOrchestrationStep
    {
        private readonly IResourceService _resourceService;
        private readonly ILogger<ImportResourcesStep> _logger;

        public ImportResourcesStep(
            IResourceService resourceService,
            ILogger<ImportResourcesStep> logger)
        {
            _resourceService = resourceService;
            _logger = logger;
        }

        public string StepName => "ImportResources";

        public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
        {
            try
            {
                var configuration = context.GetData<ExecutionConfigurationContract>("FetchedConfiguration");
                var statistics = context.GetData<ImportStatistics>("Statistics");
                var request = context.GetData<ConfigurationImportRequest>("ImportRequest");

                if (configuration?.Sequences == null)
                {
                    return StepResult.Failure("No configuration found in context");
                }

                // Extract unique resources from all sequences
                var allResources = configuration.Sequences
                    .SelectMany(s => s.Resources)
                    .GroupBy(r => r.Key)
                    .Select(g => g.First())
                    .ToList();

                // Apply filters if specified
                if (request.ResourceFilters?.Any() == true)
                {
                    allResources = allResources
                        .Where(r => request.ResourceFilters.Contains(r.Key))
                        .ToList();
                }

                foreach (var resourceContract in allResources)
                {
                    await ProcessResourceAsync(resourceContract, cancellationToken);
                    statistics.ResourcesProcessed++;
                }

                context.SetData("Statistics", statistics);

                _logger.LogInformation("Imported {Count} resources successfully", statistics.ResourcesProcessed);
                return StepResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import resources");
                return StepResult.Failure($"Failed to import resources: {ex.Message}");
            }
        }

        private async Task ProcessResourceAsync(ExecutionResourceContract resourceContract, CancellationToken cancellationToken)
        {
            // Map ExecutionResourceContract to scheduler-Data Resource entity
            var resource = new Resource
            {
                Name = resourceContract.Key,
                Code = resourceContract.Key, // Using Key as Code
                Locked = !resourceContract.HasScriptingInterface // If no scripting interface, consider it locked
            };

            // Check if resource already exists by code
            var existingResource = await _resourceService.GetByCodeAsync(resource.Code);

            if (existingResource == null)
            {
                await _resourceService.CreateResourceAsync(resource);
                _logger.LogDebug("Created new resource: {ResourceName}", resource.Name);
            }
            else
            {
                // Update existing resource
                var updatedResource = existingResource.Update(
                    name: resource.Name,
                    code: resource.Code,
                    locked: resource.Locked);
                
                await _resourceService.UpdateResourceAsync(updatedResource);
                _logger.LogDebug("Updated existing resource: {ResourceName}", resource.Name);
            }
        }
    }

    /// <summary>
    /// Step to import parameters from ExecutionService to scheduler-Data entities
    /// </summary>
    public class ImportParametersStep : IOrchestrationStep
    {
        private readonly IParameterService _parameterService;
        private readonly ILogger<ImportParametersStep> _logger;

        public ImportParametersStep(
            IParameterService parameterService,
            ILogger<ImportParametersStep> logger)
        {
            _parameterService = parameterService;
            _logger = logger;
        }

        public string StepName => "ImportParameters";

        public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
        {
            try
            {
                var configuration = context.GetData<ExecutionConfigurationContract>("FetchedConfiguration");
                var statistics = context.GetData<ImportStatistics>("Statistics");

                if (configuration?.Sequences == null)
                {
                    return StepResult.Failure("No configuration found in context");
                }

                // Extract unique parameters from all sequences
                var allParameters = configuration.Sequences
                    .SelectMany(s => s.Parameters)
                    .GroupBy(p => p.ParameterName)
                    .Select(g => g.First())
                    .ToList();

                foreach (var parameterContract in allParameters)
                {
                    await ProcessParameterAsync(parameterContract, cancellationToken);
                    statistics.ParametersProcessed++;
                }

                context.SetData("Statistics", statistics);

                _logger.LogInformation("Imported {Count} parameters successfully", statistics.ParametersProcessed);
                return StepResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import parameters");
                return StepResult.Failure($"Failed to import parameters: {ex.Message}");
            }
        }

        private async Task ProcessParameterAsync(SequenceParameterTypeContract parameterContract, CancellationToken cancellationToken)
        {
            // Map SequenceParameterTypeContract to scheduler-Data Parameter entity
            var parameter = new Parameter
            {
                Name = parameterContract.ParameterName,
                Type = MapParameterType(parameterContract.ParameterType),
                // Set reasonable defaults for scheduler-Data specific fields
                Min = null, // Could be enhanced with additional data from ExecutionService
                Max = null,
                DefaultValue = null,
                Format = null,
                RangeId = null,
                ResourceId = null
            };

            // Check if parameter already exists by name
            var existingParameters = await _parameterService.GetAllParametersAsync();
            var existingParameter = existingParameters.FirstOrDefault(p => p.Name == parameter.Name);

            if (existingParameter == null)
            {
                await _parameterService.CreateParameterAsync(parameter);
                _logger.LogDebug("Created new parameter: {ParameterName} ({ParameterType})", 
                    parameter.Name, parameter.Type);
            }
            else
            {
                // Update existing parameter if type changed
                if (existingParameter.Type != parameter.Type)
                {
                    var updatedParameter = existingParameter.Update(type: parameter.Type);
                    await _parameterService.UpdateParameterAsync(updatedParameter);
                    _logger.LogDebug("Updated parameter type: {ParameterName} -> {ParameterType}", 
                        parameter.Name, parameter.Type);
                }
            }
        }

        private ParameterType MapParameterType(ExecutionService.Contracts.ParameterType grpcParameterType)
        {
            return grpcParameterType switch
            {
                ExecutionService.Contracts.ParameterType.StringType => ParameterType.StringType,
                ExecutionService.Contracts.ParameterType.IntegerType => ParameterType.IntegerType,
                ExecutionService.Contracts.ParameterType.DecimalType => ParameterType.DecimalType,
                ExecutionService.Contracts.ParameterType.BooleanType => ParameterType.BooleanType,
                ExecutionService.Contracts.ParameterType.ArrayType => ParameterType.ArrayType,
                ExecutionService.Contracts.ParameterType.EnumType => ParameterType.EnumType,
                _ => ParameterType.StringType
            };
        }
    }

    /// <summary>
    /// Step to create sequence-parameter relationships
    /// </summary>
    public class LinkSequenceParametersStep : IOrchestrationStep
    {
        private readonly ISequenceService _sequenceService;
        private readonly IParameterService _parameterService;
        private readonly ILogger<LinkSequenceParametersStep> _logger;

        public LinkSequenceParametersStep(
            ISequenceService sequenceService,
            IParameterService parameterService,
            ILogger<LinkSequenceParametersStep> logger)
        {
            _sequenceService = sequenceService;
            _parameterService = parameterService;
            _logger = logger;
        }

        public string StepName => "LinkSequenceParameters";

        public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
        {
            try
            {
                var configuration = context.GetData<ExecutionConfigurationContract>("FetchedConfiguration");
                var statistics = context.GetData<ImportStatistics>("Statistics");

                if (configuration?.Sequences == null)
                {
                    return StepResult.Failure("No configuration found in context");
                }

                // Get all existing sequences and parameters for lookup
                var allSequences = (await _sequenceService.GetAllSequencesAsync()).ToList();
                var allParameters = (await _parameterService.GetAllParametersAsync()).ToList();

                foreach (var sequenceContract in configuration.Sequences)
                {
                    var sequence = allSequences.FirstOrDefault(s => s.Name == sequenceContract.Key);
                    if (sequence == null)
                    {
                        _logger.LogWarning("Sequence not found: {SequenceName}", sequenceContract.Key);
                        continue;
                    }

                    var orderNumber = 1;
                    foreach (var parameterContract in sequenceContract.Parameters)
                    {
                        var parameter = allParameters.FirstOrDefault(p => p.Name == parameterContract.ParameterName);
                        if (parameter == null)
                        {
                            _logger.LogWarning("Parameter not found: {ParameterName}", parameterContract.ParameterName);
                            continue;
                        }

                        try
                        {
                            await _sequenceService.AddParameterToSequenceAsync(parameter.Id, sequence.Id, orderNumber++);
                            statistics.SequenceParameterLinksCreated++;
                        }
                        catch (Exception ex)
                        {
                            // This might fail if the link already exists, which is okay
                            _logger.LogDebug("Could not add parameter {ParameterName} to sequence {SequenceName}: {Error}",
                                parameterContract.ParameterName, sequenceContract.Key, ex.Message);
                        }
                    }
                }

                context.SetData("Statistics", statistics);

                _logger.LogInformation("Created {Count} sequence-parameter links successfully", 
                    statistics.SequenceParameterLinksCreated);
                return StepResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to link sequence parameters");
                return StepResult.Failure($"Failed to link sequence parameters: {ex.Message}", shouldContinue: true);
            }
        }
    }

    /// <summary>
    /// Step to validate the imported data
    /// </summary>
    public class ValidateImportStep : IOrchestrationStep
    {
        private readonly ISequenceService _sequenceService;
        private readonly IResourceService _resourceService;
        private readonly IParameterService _parameterService;
        private readonly ILogger<ValidateImportStep> _logger;

        public ValidateImportStep(
            ISequenceService sequenceService,
            IResourceService resourceService,
            IParameterService parameterService,
            ILogger<ValidateImportStep> logger)
        {
            _sequenceService = sequenceService;
            _resourceService = resourceService;
            _parameterService = parameterService;
            _logger = logger;
        }

        public string StepName => "ValidateImport";

        public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
        {
            try
            {
                var statistics = context.GetData<ImportStatistics>("Statistics");

                // Basic validation - ensure we have some data
                var sequenceCount = (await _sequenceService.GetAllSequencesAsync()).Count();
                var resourceCount = (await _resourceService.GetAllResourcesAsync()).Count();
                var parameterCount = (await _parameterService.GetAllParametersAsync()).Count();

                _logger.LogInformation("Import validation - DB contains: {SequenceCount} sequences, {ResourceCount} resources, {ParameterCount} parameters",
                    sequenceCount, resourceCount, parameterCount);

                // Verify statistics match what's in the database (basic sanity check)
                if (statistics.SequencesProcessed > 0 && sequenceCount == 0)
                {
                    return StepResult.Failure("Statistics show sequences processed but none found in database", shouldContinue: true);
                }

                _logger.LogInformation("Import validation completed successfully");
                return StepResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate import");
                return StepResult.Failure($"Failed to validate import: {ex.Message}", shouldContinue: true);
            }
        }
    }
}

// =====================================================================================
// 10. DEPENDENCY INJECTION SETUP
// =====================================================================================

namespace Instrument.Data.Extensions
{
    using Instrument.Data.Gateway.Abstractions;
    using Instrument.Data.Gateway.Core;
    using Instrument.Data.Gateway.Configuration;
    using Instrument.Data.Gateway.Resilience;
    using Instrument.Data.Orchestration.Abstractions;
    using Instrument.Data.Orchestration.Configuration;
    using Instrument.Data.Orchestration.Steps;

    public static class GatewayAndOrchestrationExtensions
    {
        /// <summary>
        /// Register gateway and orchestration services for scheduler-Data
        /// </summary>
        public static IServiceCollection AddSchedulerDataGatewayAndOrchestration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Configuration
            services.Configure<GrpcGatewayOptions>(
                configuration.GetSection(GrpcGatewayOptions.SectionName));

            // Gateway services
            services.AddSingleton<IGrpcServiceRegistry, DiGrpcServiceRegistry>();
            services.AddSingleton<IRetryPolicy, ExponentialBackoffRetryPolicy>();
            services.AddScoped<IGrpcGateway, GrpcGateway>();

            // Orchestration services
            services.AddScoped<IProcessManager<ConfigurationImportRequest, ConfigurationImportResult>, 
                ConfigurationImportOrchestrator>();

            // Orchestration steps
            services.AddScoped<IOrchestrationStep, ValidateRequestStep>();
            services.AddScoped<IOrchestrationStep, FetchConfigurationStep>();
            services.AddScoped<IOrchestrationStep, ClearExistingDataStep>();
            services.AddScoped<IOrchestrationStep, InitializeDatabaseStep>();
            services.AddScoped<IOrchestrationStep, ImportSequencesStep>();
            services.AddScoped<IOrchestrationStep, ImportResourcesStep>();
            services.AddScoped<IOrchestrationStep, ImportParametersStep>();
            services.AddScoped<IOrchestrationStep, LinkSequenceParametersStep>();
            services.AddScoped<IOrchestrationStep, ValidateImportStep>();

            return services;
        }

        /// <summary>
        /// Register your ExecutionConfigurationService gRPC client
        /// This is where you would register your existing DI-managed gRPC client
        /// </summary>
        public static IServiceCollection AddExecutionServiceClient(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Example of how you might register your gRPC client
            // Replace this with your actual gRPC client registration
            /*
            services.AddCodeFirstGrpcClient<IExecutionConfigurationService>(options =>
            {
                options.Address = new Uri(configuration["ExecutionService:BaseAddress"]);
            });
            */

            // For now, we'll add a placeholder - you would replace this with your actual registration
            services.AddScoped<ExecutionService.Contracts.IExecutionConfigurationService, MockExecutionConfigurationService>();

            return services;
        }
    }

    /// <summary>
    /// Mock service for demonstration - replace with your actual gRPC client registration
    /// </summary>
    internal class MockExecutionConfigurationService : ExecutionService.Contracts.IExecutionConfigurationService
    {
        public Task<ExecutionService.Contracts.GetCurrentConfigurationResponse> GetCurrentConfigurationAsync(
            ExecutionService.Contracts.GetCurrentConfigurationRequest request, 
            CancellationToken cancellationToken = default)
        {
            // Return mock data for demonstration
            var mockResponse = new ExecutionService.Contracts.GetCurrentConfigurationResponse(
                RequestId: new ExecutionService.Contracts.GuidRequestId("12345", "67890"),
                Configuration: new ExecutionService.Contracts.ExecutionConfigurationContract(
                    StartingPeriod: 1,
                    RolloverPeriod: 100,
                    PeriodSpan: TimeSpan.FromMinutes(1),
                    PeriodAcceleration: 1.0,
                    Sequences: new List<ExecutionService.Contracts.ExecutionSequenceContract>
                    {
                        new("TestSequence1", TimeSpan.FromSeconds(30), "Standard", "script1", 
                            new List<ExecutionService.Contracts.ExecutionResourceContract>
                            {
                                new("Resource1", true, "StandardInterface")
                            },
                            new List<ExecutionService.Contracts.SequenceParameterTypeContract>
                            {
                                new("Param1", ExecutionService.Contracts.ParameterType.StringType),
                                new("Param2", ExecutionService.Contracts.ParameterType.IntegerType)
                            })
                    }
                ),
                Errors: new List<ExecutionService.Contracts.GrpcErrorContract>()
            );

            return Task.FromResult(mockResponse);
        }

        public Task<ExecutionService.Contracts.GetSequenceConfigurationResponse> GetSequenceConfigurationAsync(
            ExecutionService.Contracts.GetSequenceConfigurationRequest request, 
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Replace with actual implementation");
        }

        public Task<ExecutionService.Contracts.GetResourceConfigurationResponse> GetResourceConfigurationAsync(
            ExecutionService.Contracts.GetResourceConfigurationRequest request, 
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Replace with actual implementation");
        }
    }
}

// =====================================================================================
// 11. USAGE EXAMPLE
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

        /// <summary>
        /// Example of importing configuration from ExecutionService into scheduler-Data
        /// </summary>
        public async Task ImportConfigurationAsync()
        {
            var request = new ConfigurationImportRequest
            {
                IncludeSequences = true,
                ClearExistingData = true,
                SequenceFilters = new List<string> { "CriticalSequence1", "CriticalSequence2" },
                ResourceFilters = new List<string>() // Empty = import all
            };

            var result = await _processManager.ExecuteAsync(request);

            if (result.IsSuccess)
            {
                Console.WriteLine($"Import completed successfully in {result.Duration.TotalSeconds:F2}s");
                Console.WriteLine($"Request ID: {result.RequestId}");
                Console.WriteLine($"Processed: {result.Statistics.SequencesProcessed} sequences, " +
                                $"{result.Statistics.ResourcesProcessed} resources, " +
                                $"{result.Statistics.ParametersProcessed} parameters");
                Console.WriteLine($"Created {result.Statistics.SequenceParameterLinksCreated} sequence-parameter links");
                Console.WriteLine($"Steps completed: {string.Join(", ", result.ProcessedSteps)}");
            }
            else
            {
                Console.WriteLine($"Import failed: {result.ErrorMessage}");
                Console.WriteLine($"Steps completed before failure: {string.Join(", ", result.ProcessedSteps)}");
            }
        }
    }
}

// =====================================================================================
// 12. PROGRAM.CS INTEGRATION EXAMPLE
// =====================================================================================

/*
// Example Program.cs integration

using Instrument.Data;
using Instrument.Data.Configuration;
using Instrument.Data.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add your existing scheduler-Data services
var storageConfig = new StorageConfiguration
{
    Provider = StorageProviderType.SQLite,
    ConnectionString = "Data Source=scheduler.db"
};

builder.Services.AddSchedulerDataWithInitialization(storageConfig);

// Add gateway and orchestration services
builder.Services.AddSchedulerDataGatewayAndOrchestration(builder.Configuration);

// Add your ExecutionConfigurationService gRPC client
builder.Services.AddExecutionServiceClient(builder.Configuration);

var app = builder.Build();

// Example of running import on startup
using (var scope = app.Services.CreateScope())
{
    var processManager = scope.ServiceProvider
        .GetRequiredService<IProcessManager<ConfigurationImportRequest, ConfigurationImportResult>>();
    
    var importRequest = new ConfigurationImportRequest
    {
        IncludeSequences = true,
        ClearExistingData = false
    };
    
    var result = await processManager.ExecuteAsync(importRequest);
    
    if (result.IsSuccess)
    {
        Console.WriteLine($"Startup import completed: {result.Statistics.SequencesProcessed} sequences imported");
    }
    else
    {
        Console.WriteLine($"Startup import failed: {result.ErrorMessage}");
    }
}

app.Run();
*/

// =====================================================================================
// 13. CONFIGURATION EXAMPLE (appsettings.json)
// =====================================================================================

/*
{
  "GrpcGateway": {
    "DefaultTimeoutSeconds": 30,
    "MaxConcurrentRequests": 10,
    "Retry": {
      "MaxAttempts": 3,
      "BaseDelayMs": 1000,
      "BackoffMultiplier": 2.0
    },
    "Services": {
      "ExecutionConfigurationService": {
        "TimeoutSeconds": 120,
        "BaseAddress": "https://execution-service:5001"
      }
    }
  },
  "ExecutionService": {
    "BaseAddress": "https://execution-service:5001"
  }
}
*/