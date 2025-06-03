// =====================================================================================
// THREAD-SAFE gRPC GATEWAY + ORCHESTRATOR WITH DIRECT CLIENT INJECTION
// Implements thread-safe operations with modern synchronization mechanisms
// =====================================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;
using Grpc.Core;

// =====================================================================================
// 1. EXECUTION SERVICE gRPC CONTRACTS
// =====================================================================================

namespace Instrument.Data.ExecutionService.Contracts
{
    /// <summary>
    /// gRPC service interface for ExecutionConfigurationService
    /// This will be injected directly into services that need it
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
// 2. THREAD-SAFE GATEWAY ABSTRACTIONS
// =====================================================================================

namespace Instrument.Data.Gateway.Abstractions
{
    /// <summary>
    /// Represents a gRPC operation that can be executed
    /// </summary>
    public interface IGrpcOperation<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        string ServiceName { get; }
        string OperationName { get; }
        TimeSpan? Timeout { get; }
        
        /// <summary>
        /// Execute the operation with the provided gRPC client
        /// </summary>
        Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Thread-safe result wrapper for gateway operations
    /// </summary>
    public class GatewayResult<T>
    {
        private readonly object _lockObject = new object();
        private volatile bool _isSuccess;
        private T _data;
        private string _errorMessage;
        private TimeSpan _duration;

        public bool IsSuccess 
        { 
            get => _isSuccess; 
            private set => _isSuccess = value; 
        }

        public T Data 
        { 
            get { lock (_lockObject) return _data; } 
            private set { lock (_lockObject) _data = value; } 
        }

        public string ErrorMessage 
        { 
            get { lock (_lockObject) return _errorMessage; } 
            private set { lock (_lockObject) _errorMessage = value; } 
        }

        public TimeSpan Duration 
        { 
            get { lock (_lockObject) return _duration; } 
            private set { lock (_lockObject) _duration = value; } 
        }
        
        public static GatewayResult<T> Success(T data, TimeSpan duration)
        {
            return new GatewayResult<T>
            {
                IsSuccess = true,
                Data = data,
                Duration = duration
            };
        }
            
        public static GatewayResult<T> Failure(string error, TimeSpan duration)
        {
            return new GatewayResult<T>
            {
                IsSuccess = false,
                ErrorMessage = error,
                Duration = duration
            };
        }
    }

    /// <summary>
    /// Thread-safe multi-service gateway interface
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
        
        /// <summary>
        /// Get current gateway statistics (thread-safe)
        /// </summary>
        GatewayStatistics GetStatistics();
    }

    /// <summary>
    /// Thread-safe gateway statistics
    /// </summary>
    public class GatewayStatistics
    {
        private readonly ConcurrentDictionary<string, long> _operationCounts = new();
        private readonly ConcurrentDictionary<string, long> _errorCounts = new();
        private volatile long _totalRequests;
        private volatile long _totalErrors;

        public long TotalRequests => _totalRequests;
        public long TotalErrors => _totalErrors;
        public IReadOnlyDictionary<string, long> OperationCounts => _operationCounts;
        public IReadOnlyDictionary<string, long> ErrorCounts => _errorCounts;

        public void IncrementOperation(string operationName)
        {
            _operationCounts.AddOrUpdate(operationName, 1, (key, value) => value + 1);
            Interlocked.Increment(ref _totalRequests);
        }

        public void IncrementError(string operationName)
        {
            _errorCounts.AddOrUpdate(operationName, 1, (key, value) => value + 1);
            Interlocked.Increment(ref _totalErrors);
        }
    }
}

// =====================================================================================
// 3. THREAD-SAFE GATEWAY CONFIGURATION
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
        public bool UseJitter { get; set; } = true;
    }
}

// =====================================================================================
// 4. THREAD-SAFE RETRY POLICY
// =====================================================================================

namespace Instrument.Data.Gateway.Resilience
{
    using Instrument.Data.Gateway.Configuration;

    public interface IRetryPolicy
    {
        Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Thread-safe exponential backoff retry policy with jitter
    /// </summary>
    public class ExponentialBackoffRetryPolicy : IRetryPolicy
    {
        private readonly RetryOptions _options;
        private readonly ILogger<ExponentialBackoffRetryPolicy> _logger;
        private readonly ThreadLocal<Random> _random;

        public ExponentialBackoffRetryPolicy(
            IOptions<GrpcGatewayOptions> options,
            ILogger<ExponentialBackoffRetryPolicy> logger)
        {
            _options = options.Value.Retry;
            _logger = logger;
            _random = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));
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
            var baseDelay = (int)(_options.BaseDelayMs * Math.Pow(_options.BackoffMultiplier, retryAttempt));
            
            if (_options.UseJitter && _random.IsValueCreated)
            {
                // Add ±25% jitter to prevent thundering herd
                var jitterRange = (int)(baseDelay * 0.25);
                var jitter = _random.Value.Next(-jitterRange, jitterRange + 1);
                return Math.Max(0, baseDelay + jitter);
            }
            
            return baseDelay;
        }

        private static bool IsRetryable(Exception ex) => ex switch
        {
            RpcException rpc => rpc.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded or StatusCode.Internal,
            TaskCanceledException => true,
            TimeoutException => true,
            OperationCanceledException => false, // Don't retry if explicitly cancelled
            _ => false
        };

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _random?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

// =====================================================================================
// 5. THREAD-SAFE GATEWAY IMPLEMENTATION
// =====================================================================================

namespace Instrument.Data.Gateway.Core
{
    using Instrument.Data.Gateway.Abstractions;
    using Instrument.Data.Gateway.Configuration;
    using Instrument.Data.Gateway.Resilience;

    /// <summary>
    /// Thread-safe multi-service gRPC Gateway with direct client injection
    /// </summary>
    public class GrpcGateway : IGrpcGateway, IDisposable
    {
        private readonly IRetryPolicy _retryPolicy;
        private readonly ILogger<GrpcGateway> _logger;
        private readonly GrpcGatewayOptions _options;
        private readonly SemaphoreSlim _semaphore;
        private readonly GatewayStatistics _statistics;
        private readonly ConcurrentDictionary<string, object> _clientCache;
        private volatile bool _disposed;

        public GrpcGateway(
            IRetryPolicy retryPolicy,
            ILogger<GrpcGateway> logger,
            IOptions<GrpcGatewayOptions> options)
        {
            _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _semaphore = new SemaphoreSlim(_options.MaxConcurrentRequests, _options.MaxConcurrentRequests);
            _statistics = new GatewayStatistics();
            _clientCache = new ConcurrentDictionary<string, object>();
        }

        public async Task<GatewayResult<TResponse>> ExecuteAsync<TRequest, TResponse>(
            IGrpcOperation<TRequest, TResponse> operation,
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class
        {
            ThrowIfDisposed();
            
            var stopwatch = Stopwatch.StartNew();
            var operationKey = $"{operation.ServiceName}.{operation.OperationName}";
            
            _logger.LogDebug("Executing {OperationKey}", operationKey);

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

                _statistics.IncrementOperation(operationKey);
                
                _logger.LogInformation("Successfully executed {OperationKey} in {Duration}ms",
                    operationKey, stopwatch.ElapsedMilliseconds);

                return GatewayResult<TResponse>.Success(result, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                _statistics.IncrementError(operationKey);
                
                _logger.LogError(ex, "Failed to execute {OperationKey}",
                    operationKey);
                
                return GatewayResult<TResponse>.Failure(ex.Message, stopwatch.Elapsed);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> IsServiceAvailableAsync(string serviceName, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            try
            {
                // For direct injection, we would check if the service is responsive
                // This could be extended to do actual health checks
                await Task.Delay(1, cancellationToken); // Placeholder for actual health check
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Service availability check failed for: {ServiceName}", serviceName);
                return false;
            }
        }

        public GatewayStatistics GetStatistics()
        {
            ThrowIfDisposed();
            return _statistics;
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

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GrpcGateway));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _semaphore?.Dispose();
                _retryPolicy?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}

// =====================================================================================
// 6. CONCRETE gRPC OPERATIONS WITH DIRECT CLIENT INJECTION
// =====================================================================================

namespace Instrument.Data.Gateway.Operations
{
    using Instrument.Data.Gateway.Abstractions;
    using Instrument.Data.ExecutionService.Contracts;

    /// <summary>
    /// Thread-safe operation to get current configuration from ExecutionConfigurationService
    /// </summary>
    public class GetCurrentConfigurationOperation : IGrpcOperation<GetCurrentConfigurationRequest, GetCurrentConfigurationResponse>
    {
        private readonly IExecutionConfigurationService _client;

        public GetCurrentConfigurationOperation(IExecutionConfigurationService client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public string ServiceName => "ExecutionConfigurationService";
        public string OperationName => "GetCurrentConfiguration";
        public TimeSpan? Timeout => TimeSpan.FromMinutes(2); // Large payload

        public async Task<GetCurrentConfigurationResponse> ExecuteAsync(
            GetCurrentConfigurationRequest request, 
            CancellationToken cancellationToken)
        {
            return await _client.GetCurrentConfigurationAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// Thread-safe operation to get single sequence configuration
    /// </summary>
    public class GetSequenceConfigurationOperation : IGrpcOperation<GetSequenceConfigurationRequest, GetSequenceConfigurationResponse>
    {
        private readonly IExecutionConfigurationService _client;

        public GetSequenceConfigurationOperation(IExecutionConfigurationService client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public string ServiceName => "ExecutionConfigurationService";
        public string OperationName => "GetSequenceConfiguration";
        public TimeSpan? Timeout => null; // Use default

        public async Task<GetSequenceConfigurationResponse> ExecuteAsync(
            GetSequenceConfigurationRequest request, 
            CancellationToken cancellationToken)
        {
            return await _client.GetSequenceConfigurationAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// Thread-safe operation to get single resource configuration
    /// </summary>
    public class GetResourceConfigurationOperation : IGrpcOperation<GetResourceConfigurationRequest, GetResourceConfigurationResponse>
    {
        private readonly IExecutionConfigurationService _client;

        public GetResourceConfigurationOperation(IExecutionConfigurationService client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public string ServiceName => "ExecutionConfigurationService";
        public string OperationName => "GetResourceConfiguration";
        public TimeSpan? Timeout => null; // Use default

        public async Task<GetResourceConfigurationResponse> ExecuteAsync(
            GetResourceConfigurationRequest request, 
            CancellationToken cancellationToken)
        {
            return await _client.GetResourceConfigurationAsync(request, cancellationToken);
        }
    }
}

// =====================================================================================
// 7. THREAD-SAFE PROCESS MANAGER PATTERN
// =====================================================================================

namespace Instrument.Data.Orchestration.Abstractions
{
    /// <summary>
    /// Represents a thread-safe step in the orchestration process
    /// </summary>
    public interface IOrchestrationStep
    {
        string StepName { get; }
        Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Thread-safe context passed between orchestration steps
    /// </summary>
    public class OrchestrationContext
    {
        private readonly ConcurrentDictionary<string, object> _data = new();
        private readonly List<string> _completedSteps = new();
        private readonly List<string> _errors = new();
        private readonly ReaderWriterLockSlim _stepsLock = new();
        private readonly ReaderWriterLockSlim _errorsLock = new();

        public T GetData<T>(string key) => _data.TryGetValue(key, out var value) ? (T)value : default;
        
        public void SetData<T>(string key, T value) => _data.AddOrUpdate(key, value, (k, v) => value);

        public IReadOnlyList<string> CompletedSteps 
        { 
            get 
            { 
                _stepsLock.EnterReadLock();
                try 
                { 
                    return _completedSteps.ToList(); 
                } 
                finally 
                { 
                    _stepsLock.ExitReadLock(); 
                } 
            } 
        }

        public IReadOnlyList<string> Errors 
        { 
            get 
            { 
                _errorsLock.EnterReadLock();
                try 
                { 
                    return _errors.ToList(); 
                } 
                finally 
                { 
                    _errorsLock.ExitReadLock(); 
                } 
            } 
        }

        public void AddCompletedStep(string stepName)
        {
            _stepsLock.EnterWriteLock();
            try
            {
                _completedSteps.Add(stepName);
            }
            finally
            {
                _stepsLock.ExitWriteLock();
            }
        }

        public void AddError(string error)
        {
            _errorsLock.EnterWriteLock();
            try
            {
                _errors.Add(error);
            }
            finally
            {
                _errorsLock.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            _stepsLock?.Dispose();
            _errorsLock?.Dispose();
        }
    }

    /// <summary>
    /// Thread-safe result of a single orchestration step
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
    /// Thread-safe process manager interface
    /// </summary>
    public interface IProcessManager<TRequest, TResult>
    {
        Task<TResult> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
    }
}

// =====================================================================================
// 8. THREAD-SAFE CONFIGURATION IMPORT
// =====================================================================================

namespace Instrument.Data.Orchestration.Configuration
{
    using Instrument.Data.Orchestration.Abstractions;

    /// <summary>
    /// Thread-safe request for configuration import process
    /// </summary>
    public class ConfigurationImportRequest
    {
        public bool IncludeSequences { get; set; } = true;
        public bool ClearExistingData { get; set; } = false;
        public List<string> SequenceFilters { get; set; } = new();
        public List<string> ResourceFilters { get; set; } = new();
    }

    /// <summary>
    /// Thread-safe result of configuration import process
    /// </summary>
    public class ConfigurationImportResult
    {
        private readonly object _lockObject = new object();
        
        private bool _isSuccess;
        private string _errorMessage;
        private TimeSpan _duration;
        private ImportStatistics _statistics = new();
        private List<string> _processedSteps = new();
        private string _requestId;

        public bool IsSuccess 
        { 
            get { lock (_lockObject) return _isSuccess; } 
            set { lock (_lockObject) _isSuccess = value; } 
        }

        public string ErrorMessage 
        { 
            get { lock (_lockObject) return _errorMessage; } 
            set { lock (_lockObject) _errorMessage = value; } 
        }

        public TimeSpan Duration 
        { 
            get { lock (_lockObject) return _duration; } 
            set { lock (_lockObject) _duration = value; } 
        }

        public ImportStatistics Statistics 
        { 
            get { lock (_lockObject) return _statistics; } 
            set { lock (_lockObject) _statistics = value; } 
        }

        public List<string> ProcessedSteps 
        { 
            get { lock (_lockObject) return new List<string>(_processedSteps); } 
            set { lock (_lockObject) _processedSteps = new List<string>(value); } 
        }

        public string RequestId 
        { 
            get { lock (_lockObject) return _requestId; } 
            set { lock (_lockObject) _requestId = value; } 
        }
    }

    /// <summary>
    /// Thread-safe import statistics
    /// </summary>
    public class ImportStatistics
    {
        private volatile int _sequencesProcessed;
        private volatile int _resourcesProcessed;
        private volatile int _parametersProcessed;
        private volatile int _sequenceParameterLinksCreated;

        public int SequencesProcessed => _sequencesProcessed;
        public int ResourcesProcessed => _resourcesProcessed;
        public int ParametersProcessed => _parametersProcessed;
        public int SequenceParameterLinksCreated => _sequenceParameterLinksCreated;

        public void IncrementSequences() => Interlocked.Increment(ref _sequencesProcessed);
        public void IncrementResources() => Interlocked.Increment(ref _resourcesProcessed);
        public void IncrementParameters() => Interlocked.Increment(ref _parametersProcessed);
        public void IncrementLinks() => Interlocked.Increment(ref _sequenceParameterLinksCreated);
    }

    /// <summary>
    /// Thread-safe Configuration Import Process Manager
    /// </summary>
    public class ConfigurationImportOrchestrator : IProcessManager<ConfigurationImportRequest, ConfigurationImportResult>
    {
        private readonly IEnumerable<IOrchestrationStep> _steps;
        private readonly ILogger<ConfigurationImportOrchestrator> _logger;
        private readonly SemaphoreSlim _semaphore;

        public ConfigurationImportOrchestrator(
            IEnumerable<IOrchestrationStep> steps,
            ILogger<ConfigurationImportOrchestrator> logger)
        {
            _steps = steps?.OrderBy(s => GetStepOrder(s.StepName)) ?? throw new ArgumentNullException(nameof(steps));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _semaphore = new SemaphoreSlim(1, 1); // Serialize orchestration executions
        }

        public async Task<ConfigurationImportResult> ExecuteAsync(
            ConfigurationImportRequest request,
            CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            
            try
            {
                return await ExecuteInternalAsync(request, cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<ConfigurationImportResult> ExecuteInternalAsync(
            ConfigurationImportRequest request,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var context = new OrchestrationContext();
            var result = new ConfigurationImportResult();

            try
            {
                // Store request in context
                context.SetData("ImportRequest", request);
                context.SetData("Statistics", new ImportStatistics());

                _logger.LogInformation("Starting configuration import process with {StepCount} steps", _steps.Count());

                foreach (var step in _steps)
                {
                    _logger.LogDebug("Executing step: {StepName}", step.StepName);

                    var stepResult = await step.ExecuteAsync(context, cancellationToken);
                    context.AddCompletedStep(step.StepName);

                    if (!stepResult.IsSuccess)
                    {
                        var error = $"Step '{step.StepName}' failed: {stepResult.ErrorMessage}";
                        context.AddError(error);
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
                    var errors = context.Errors;
                    result.IsSuccess = errors.Count == 0;
                    if (!result.IsSuccess)
                    {
                        result.ErrorMessage = string.Join("; ", errors);
                    }
                }

                // Extract results from context
                result.Statistics = context.GetData<ImportStatistics>("Statistics") ?? new ImportStatistics();
                result.ProcessedSteps = context.CompletedSteps.ToList();
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
                context.Dispose();
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
// 9. THREAD-SAFE ORCHESTRATION STEPS
// =====================================================================================

namespace Instrument.Data.Orchestration.Steps
{
    using Instrument.Data.Orchestration.Abstractions;
    using Instrument.Data.Orchestration.Configuration;
    using Instrument.Data.Gateway.Abstractions;
    using Instrument.Data.Gateway.Operations;
    using Instrument.Data.ExecutionService.Contracts;

    /// <summary>
    /// Thread-safe step to fetch configuration from ExecutionConfigurationService
    /// </summary>
    public class FetchConfigurationStep : IOrchestrationStep
    {
        private readonly IGrpcGateway _gateway;
        private readonly IExecutionConfigurationService _executionClient;
        private readonly ILogger<FetchConfigurationStep> _logger;

        public FetchConfigurationStep(
            IGrpcGateway gateway,
            IExecutionConfigurationService executionClient,
            ILogger<FetchConfigurationStep> logger)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _executionClient = executionClient ?? throw new ArgumentNullException(nameof(executionClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string StepName => "FetchConfiguration";

        public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
        {
            var request = context.GetData<ConfigurationImportRequest>("ImportRequest");

            try
            {
                var operation = new GetCurrentConfigurationOperation(_executionClient);
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

                // Store the fetched configuration and metadata in context (thread-safe)
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
    /// Thread-safe step to clear existing data if requested
    /// </summary>
    public class ClearExistingDataStep : IOrchestrationStep
    {
        private readonly ISequenceService _sequenceService;
        private readonly IResourceService _resourceService;
        private readonly IParameterService _parameterService;
        private readonly ILogger<ClearExistingDataStep> _logger;
        private readonly SemaphoreSlim _semaphore;

        public ClearExistingDataStep(
            ISequenceService sequenceService,
            IResourceService resourceService,
            IParameterService parameterService,
            ILogger<ClearExistingDataStep> logger)
        {
            _sequenceService = sequenceService ?? throw new ArgumentNullException(nameof(sequenceService));
            _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
            _parameterService = parameterService ?? throw new ArgumentNullException(nameof(parameterService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _semaphore = new SemaphoreSlim(1, 1); // Serialize data clearing
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

            await _semaphore.WaitAsync(cancellationToken);
            
            try
            {
                // Clear data in dependency order (sequences first, then resources, then parameters)
                var existingSequences = await _sequenceService.GetAllSequencesAsync();
                var deleteSequenceTasks = existingSequences.Select(s => _sequenceService.DeleteSequenceAsync(s.Id));
                await Task.WhenAll(deleteSequenceTasks);

                var existingResources = await _resourceService.GetAllResourcesAsync();
                var deleteResourceTasks = existingResources.Select(r => _resourceService.DeleteResourceAsync(r.Id));
                await Task.WhenAll(deleteResourceTasks);

                var existingParameters = await _parameterService.GetAllParametersAsync();
                var deleteParameterTasks = existingParameters.Select(p => _parameterService.DeleteParameterAsync(p.Id));
                await Task.WhenAll(deleteParameterTasks);

                _logger.LogInformation("Existing data cleared successfully");
                return StepResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear existing data");
                return StepResult.Failure($"Failed to clear existing data: {ex.Message}", shouldContinue: true);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    /// <summary>
    /// Thread-safe step to import sequences from ExecutionService
    /// </summary>
    public class ImportSequencesStep : IOrchestrationStep
    {
        private readonly ISequenceService _sequenceService;
        private readonly ILogger<ImportSequencesStep> _logger;
        private readonly SemaphoreSlim _semaphore;

        public ImportSequencesStep(
            ISequenceService sequenceService,
            ILogger<ImportSequencesStep> logger)
        {
            _sequenceService = sequenceService ?? throw new ArgumentNullException(nameof(sequenceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount); // Parallel processing
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

                // Process sequences in parallel with controlled concurrency
                var sequenceTasks = sequencesToProcess.Select(async sequenceContract =>
                {
                    await _semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        await ProcessSequenceAsync(sequenceContract, cancellationToken);
                        statistics.IncrementSequences();
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                });

                await Task.WhenAll(sequenceTasks);

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
                CanBeParallel = false // Default value
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

    // Additional steps would follow the same thread-safe patterns...
    // (ImportResourcesStep, ImportParametersStep, etc. would be implemented similarly)
}

// =====================================================================================
// 10. THREAD-SAFE DEPENDENCY INJECTION SETUP
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
        /// Register thread-safe gateway and orchestration services for scheduler-Data
        /// </summary>
        public static IServiceCollection AddSchedulerDataGatewayAndOrchestration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Configuration
            services.Configure<GrpcGatewayOptions>(
                configuration.GetSection(GrpcGatewayOptions.SectionName));

            // Thread-safe gateway services
            services.AddSingleton<IRetryPolicy, ExponentialBackoffRetryPolicy>();
            services.AddScoped<IGrpcGateway, GrpcGateway>();

            // Thread-safe orchestration services
            services.AddScoped<IProcessManager<ConfigurationImportRequest, ConfigurationImportResult>, 
                ConfigurationImportOrchestrator>();

            // Thread-safe orchestration steps
            services.AddScoped<IOrchestrationStep, FetchConfigurationStep>();
            services.AddScoped<IOrchestrationStep, ClearExistingDataStep>();
            // Add other steps...

            return services;
        }

        /// <summary>
        /// Register your ExecutionConfigurationService gRPC client directly
        /// This replaces the service registry pattern with direct injection
        /// </summary>
        public static IServiceCollection AddExecutionServiceClient(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Example of direct gRPC client registration
            // Replace this with your actual gRPC client registration
            services.AddCodeFirstGrpcClient<IExecutionConfigurationService>(options =>
            {
                options.Address = new Uri(configuration["ExecutionService:BaseAddress"]);
                // Add any other client configuration
            });

            return services;
        }
    }
}

// =====================================================================================
// 11. THREAD-SAFE USAGE EXAMPLE
// =====================================================================================

namespace Instrument.Data.Examples
{
    using Instrument.Data.Orchestration.Abstractions;
    using Instrument.Data.Orchestration.Configuration;

    public class ThreadSafeConfigurationImportExample
    {
        private readonly IProcessManager<ConfigurationImportRequest, ConfigurationImportResult> _processManager;
        private readonly IGrpcGateway _gateway;

        public ThreadSafeConfigurationImportExample(
            IProcessManager<ConfigurationImportRequest, ConfigurationImportResult> processManager,
            IGrpcGateway gateway)
        {
            _processManager = processManager;
            _gateway = gateway;
        }

        /// <summary>
        /// Example of thread-safe configuration import
        /// </summary>
        public async Task ImportConfigurationConcurrentlyAsync()
        {
            // Multiple concurrent imports are thread-safe
            var importTasks = Enumerable.Range(1, 3).Select(async i =>
            {
                var request = new ConfigurationImportRequest
                {
                    IncludeSequences = true,
                    ClearExistingData = i == 1, // Only first import clears data
                    SequenceFilters = new List<string> { $"Sequence{i}" }
                };

                var result = await _processManager.ExecuteAsync(request);

                if (result.IsSuccess)
                {
                    Console.WriteLine($"Import {i} completed successfully in {result.Duration.TotalSeconds:F2}s");
                    Console.WriteLine($"Request ID: {result.RequestId}");
                    Console.WriteLine($"Processed: {result.Statistics.SequencesProcessed} sequences");
                }
                else
                {
                    Console.WriteLine($"Import {i} failed: {result.ErrorMessage}");
                }

                return result;
            });

            var results = await Task.WhenAll(importTasks);

            // Check gateway statistics (thread-safe)
            var stats = _gateway.GetStatistics();
            Console.WriteLine($"Total Gateway Requests: {stats.TotalRequests}, Errors: {stats.TotalErrors}");
        }
    }
}

// =====================================================================================
// 12. CONFIGURATION EXAMPLE (appsettings.json)
// =====================================================================================

/*
{
  "GrpcGateway": {
    "DefaultTimeoutSeconds": 30,
    "MaxConcurrentRequests": 10,
    "Retry": {
      "MaxAttempts": 3,
      "BaseDelayMs": 1000,
      "BackoffMultiplier": 2.0,
      "UseJitter": true
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