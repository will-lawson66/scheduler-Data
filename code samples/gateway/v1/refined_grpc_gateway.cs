// =====================================================================================
// TRUE gRPC GATEWAY - MULTI-SERVICE ROUTING WITH TRequest, TResult
// =====================================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Grpc.Core;
using Grpc.Net.Client;

// =====================================================================================
// 1. CONFIGURATION FOR MULTIPLE SERVICES
// =====================================================================================

namespace Instrument.Data.Configuration
{
    /// <summary>
    /// Configuration for the gRPC Gateway supporting multiple services
    /// </summary>
    public class GrpcGatewayOptions
    {
        public const string SectionName = "GrpcGateway";

        /// <summary>
        /// Default settings applied to all services
        /// </summary>
        public GrpcServiceDefaults Defaults { get; set; } = new GrpcServiceDefaults();

        /// <summary>
        /// Configuration for individual gRPC services
        /// Key = service name, Value = service configuration
        /// </summary>
        public Dictionary<string, GrpcServiceConfig> Services { get; set; } = new Dictionary<string, GrpcServiceConfig>();

        /// <summary>
        /// Global gateway settings
        /// </summary>
        public int MaxConcurrentRequests { get; set; } = 20;
        public bool EnableCaching { get; set; } = true;
        public int CacheExpiryMinutes { get; set; } = 15;
        public RetryPolicyOptions RetryPolicy { get; set; } = new RetryPolicyOptions();
        public CircuitBreakerOptions CircuitBreaker { get; set; } = new CircuitBreakerOptions();
    }

    /// <summary>
    /// Default settings for gRPC services
    /// </summary>
    public class GrpcServiceDefaults
    {
        public bool UseSecureConnection { get; set; } = true;
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxConnectionsPerService { get; set; } = 5;
    }

    /// <summary>
    /// Configuration for a specific gRPC service
    /// </summary>
    public class GrpcServiceConfig
    {
        /// <summary>
        /// Base address of the gRPC service
        /// </summary>
        public string BaseAddress { get; set; }

        /// <summary>
        /// Service-specific overrides (null = use defaults)
        /// </summary>
        public bool? UseSecureConnection { get; set; }
        public int? TimeoutSeconds { get; set; }
        public int? MaxConnectionsPerService { get; set; }

        /// <summary>
        /// Load balancing endpoints (if multiple instances)
        /// </summary>
        public List<string> Endpoints { get; set; } = new List<string>();

        /// <summary>
        /// Service-specific retry policy overrides
        /// </summary>
        public RetryPolicyOptions RetryPolicy { get; set; }

        /// <summary>
        /// Service-specific circuit breaker overrides
        /// </summary>
        public CircuitBreakerOptions CircuitBreaker { get; set; }
    }

    public class RetryPolicyOptions
    {
        public int MaxRetries { get; set; } = 3;
        public int InitialDelayMs { get; set; } = 1000;
        public double BackoffMultiplier { get; set; } = 2.0;
        public int MaxDelayMs { get; set; } = 30000;
        public bool UseJitter { get; set; } = true;
    }

    public class CircuitBreakerOptions
    {
        public int FailureThreshold { get; set; } = 5;
        public int OpenToHalfOpenTimeoutSeconds { get; set; } = 30;
        public int SuccessThreshold { get; set; } = 2;
    }
}

// =====================================================================================
// 2. GENERIC OPERATION WITH TRequest, TResult
// =====================================================================================

namespace Instrument.Data.Gateways
{
    /// <summary>
    /// Generic gRPC operation with explicit request and result types
    /// </summary>
    public abstract class GrpcOperation<TRequest, TResult>
        where TRequest : class
        where TResult : class
    {
        /// <summary>
        /// Which gRPC service this operation targets
        /// </summary>
        public abstract string ServiceName { get; }

        /// <summary>
        /// Unique identifier for this operation type
        /// </summary>
        public abstract string OperationId { get; }

        /// <summary>
        /// Whether results should be cached
        /// </summary>
        public virtual bool IsCacheable { get; } = true;

        /// <summary>
        /// Custom cache key generation (default uses OperationId + Request hash)
        /// </summary>
        public virtual string GenerateCacheKey(TRequest request)
        {
            var requestHash = request?.GetHashCode() ?? 0;
            return $"{ServiceName}_{OperationId}_{requestHash}";
        }

        /// <summary>
        /// Operation-specific timeout override
        /// </summary>
        public virtual TimeSpan? Timeout { get; } = null;

        /// <summary>
        /// Execute the gRPC operation
        /// </summary>
        /// <param name="channel">gRPC channel to the target service</param>
        /// <param name="request">Request data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        public abstract Task<TResult> ExecuteAsync(GrpcChannel channel, TRequest request, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Batch operation for multiple requests of the same type
    /// </summary>
    public class BatchGrpcOperation<TRequest, TResult>
        where TRequest : class
        where TResult : class
    {
        /// <summary>
        /// The operation template to execute
        /// </summary>
        public GrpcOperation<TRequest, TResult> Operation { get; set; }

        /// <summary>
        /// List of requests to process
        /// </summary>
        public IEnumerable<TRequest> Requests { get; set; }

        /// <summary>
        /// Maximum concurrent executions
        /// </summary>
        public int MaxConcurrency { get; set; } = 5;

        /// <summary>
        /// Whether to continue processing if one request fails
        /// </summary>
        public bool ContinueOnError { get; set; } = true;

        /// <summary>
        /// Batch identifier for tracking
        /// </summary>
        public string BatchId { get; set; } = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Result wrapper for individual operations
    /// </summary>
    public class GrpcOperationResult<TResult> where TResult : class
    {
        public bool Success { get; set; }
        public TResult Data { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public bool FromCache { get; set; }
        public string ServiceName { get; set; }
        public string OperationId { get; set; }
    }

    /// <summary>
    /// Result wrapper for batch operations
    /// </summary>
    public class BatchGrpcOperationResult<TResult> where TResult : class
    {
        public string BatchId { get; set; }
        public string ServiceName { get; set; }
        public string OperationId { get; set; }
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public List<GrpcOperationResult<TResult>> Results { get; set; } = new List<GrpcOperationResult<TResult>>();
        public TimeSpan TotalExecutionTime { get; set; }
    }
}

// =====================================================================================
// 3. SERVICE ROUTING AND CHANNEL MANAGEMENT
// =====================================================================================

namespace Instrument.Data.Gateways
{
    /// <summary>
    /// Manages gRPC channels for multiple services with load balancing
    /// </summary>
    public interface IGrpcChannelManager
    {
        /// <summary>
        /// Get a channel for the specified service
        /// </summary>
        GrpcChannel GetChannel(string serviceName);

        /// <summary>
        /// Test connectivity to a service
        /// </summary>
        Task<bool> TestServiceConnectionAsync(string serviceName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get health status for all services
        /// </summary>
        Task<Dictionary<string, ServiceHealthStatus>> GetAllServiceHealthAsync(CancellationToken cancellationToken = default);
    }

    public class GrpcChannelManager : IGrpcChannelManager, IDisposable
    {
        private readonly GrpcGatewayOptions _options;
        private readonly ILogger<GrpcChannelManager> _logger;
        private readonly ConcurrentDictionary<string, GrpcChannel> _channels = new();
        private readonly ConcurrentDictionary<string, RoundRobinEndpointSelector> _endpointSelectors = new();

        public GrpcChannelManager(IOptions<GrpcGatewayOptions> options, ILogger<GrpcChannelManager> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public GrpcChannel GetChannel(string serviceName)
        {
            return _channels.GetOrAdd(serviceName, CreateChannelForService);
        }

        private GrpcChannel CreateChannelForService(string serviceName)
        {
            if (!_options.Services.TryGetValue(serviceName, out var serviceConfig))
            {
                throw new InvalidOperationException($"Service '{serviceName}' is not configured");
            }

            // Determine the endpoint to use
            string endpoint;
            if (serviceConfig.Endpoints?.Any() == true)
            {
                // Use load balancing if multiple endpoints
                var selector = _endpointSelectors.GetOrAdd(serviceName, 
                    _ => new RoundRobinEndpointSelector(serviceConfig.Endpoints));
                endpoint = selector.GetNext();
            }
            else
            {
                endpoint = serviceConfig.BaseAddress;
            }

            if (string.IsNullOrEmpty(endpoint))
            {
                throw new InvalidOperationException($"No endpoint configured for service '{serviceName}'");
            }

            // Create channel options
            var channelOptions = new GrpcChannelOptions
            {
                MaxReceiveMessageSize = null,
                MaxSendMessageSize = null,
            };

            // Determine security settings
            var useSecure = serviceConfig.UseSecureConnection ?? _options.Defaults.UseSecureConnection;
            if (!useSecure)
            {
                _logger.LogWarning("Using insecure connection for service: {ServiceName}", serviceName);
                channelOptions.HttpHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
            }

            var channel = GrpcChannel.ForAddress(endpoint, channelOptions);
            _logger.LogInformation("Created gRPC channel for service {ServiceName} at {Endpoint}", serviceName, endpoint);

            return channel;
        }

        public async Task<bool> TestServiceConnectionAsync(string serviceName, CancellationToken cancellationToken = default)
        {
            try
            {
                var channel = GetChannel(serviceName);
                await channel.ConnectAsync(cancellationToken);
                return channel.State == ConnectivityState.Ready;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Connection test failed for service: {ServiceName}", serviceName);
                return false;
            }
        }

        public async Task<Dictionary<string, ServiceHealthStatus>> GetAllServiceHealthAsync(CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, ServiceHealthStatus>();

            var tasks = _options.Services.Keys.Select(async serviceName =>
            {
                var isConnected = await TestServiceConnectionAsync(serviceName, cancellationToken);
                return new { ServiceName = serviceName, IsConnected = isConnected };
            });

            var healthResults = await Task.WhenAll(tasks);

            foreach (var result in healthResults)
            {
                results[result.ServiceName] = new ServiceHealthStatus
                {
                    ServiceName = result.ServiceName,
                    IsConnected = result.IsConnected,
                    Timestamp = DateTime.UtcNow,
                    Endpoint = _options.Services[result.ServiceName].BaseAddress
                };
            }

            return results;
        }

        public void Dispose()
        {
            foreach (var channel in _channels.Values)
            {
                channel?.Dispose();
            }
            _channels.Clear();
        }
    }

    /// <summary>
    /// Simple round-robin endpoint selector for load balancing
    /// </summary>
    public class RoundRobinEndpointSelector
    {
        private readonly List<string> _endpoints;
        private int _currentIndex = 0;
        private readonly object _lock = new object();

        public RoundRobinEndpointSelector(List<string> endpoints)
        {
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            if (!_endpoints.Any())
                throw new ArgumentException("At least one endpoint must be provided", nameof(endpoints));
        }

        public string GetNext()
        {
            lock (_lock)
            {
                var endpoint = _endpoints[_currentIndex];
                _currentIndex = (_currentIndex + 1) % _endpoints.Count;
                return endpoint;
            }
        }
    }

    public class ServiceHealthStatus
    {
        public string ServiceName { get; set; }
        public bool IsConnected { get; set; }
        public DateTime Timestamp { get; set; }
        public string Endpoint { get; set; }
    }
}

// =====================================================================================
// 4. RESILIENCE COMPONENTS (SAME AS BEFORE)
// =====================================================================================

namespace Instrument.Data.Resilience
{
    // [CustomRetryPolicy and CircuitBreaker classes remain the same as in previous version]
    // Including here for completeness but not duplicating the code
    
    public class CustomRetryPolicy
    {
        private readonly RetryPolicyOptions _options;
        private readonly ILogger<CustomRetryPolicy> _logger;
        private readonly Random _random = new Random();

        public CustomRetryPolicy(RetryPolicyOptions options, ILogger<CustomRetryPolicy> logger)
        {
            _options = options;
            _logger = logger;
        }

        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            Exception lastException = null;
            for (int attempt = 0; attempt <= _options.MaxRetries; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        var delay = CalculateDelay(attempt);
                        await Task.Delay(delay, cancellationToken);
                    }
                    return await operation(cancellationToken);
                }
                catch (Exception ex) when (IsRetriableException(ex) && attempt < _options.MaxRetries)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "Attempt {Attempt} failed with retriable error", attempt + 1);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Operation failed on attempt {Attempt}", attempt + 1);
                    throw;
                }
            }
            throw lastException ?? new InvalidOperationException("All retry attempts failed");
        }

        private int CalculateDelay(int attempt)
        {
            var exponentialDelay = (int)(_options.InitialDelayMs * Math.Pow(_options.BackoffMultiplier, attempt - 1));
            var cappedDelay = Math.Min(exponentialDelay, _options.MaxDelayMs);
            if (_options.UseJitter)
            {
                var jitterRange = (int)(cappedDelay * 0.25);
                var jitter = _random.Next(-jitterRange, jitterRange + 1);
                cappedDelay = Math.Max(0, cappedDelay + jitter);
            }
            return cappedDelay;
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
                _ => false
            };
        }
    }

    public class CircuitBreaker
    {
        private readonly CircuitBreakerOptions _options;
        private readonly ILogger<CircuitBreaker> _logger;
        private readonly object _lock = new object();
        private CircuitBreakerState _state = CircuitBreakerState.Closed;
        private int _failureCount = 0;
        private int _successCount = 0;
        private DateTime _lastFailureTime = DateTime.MinValue;

        public CircuitBreaker(CircuitBreakerOptions options, ILogger<CircuitBreaker> logger)
        {
            _options = options;
            _logger = logger;
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            lock (_lock)
            {
                if (_state == CircuitBreakerState.Open)
                {
                    if (DateTime.UtcNow - _lastFailureTime > TimeSpan.FromSeconds(_options.OpenToHalfOpenTimeoutSeconds))
                    {
                        _state = CircuitBreakerState.HalfOpen;
                        _successCount = 0;
                    }
                    else
                    {
                        throw new CircuitBreakerOpenException("Circuit breaker is open");
                    }
                }
            }

            try
            {
                var result = await operation();
                OnSuccess();
                return result;
            }
            catch (Exception ex)
            {
                OnFailure(ex);
                throw;
            }
        }

        private void OnSuccess()
        {
            lock (_lock)
            {
                _failureCount = 0;
                if (_state == CircuitBreakerState.HalfOpen)
                {
                    _successCount++;
                    if (_successCount >= _options.SuccessThreshold)
                    {
                        _state = CircuitBreakerState.Closed;
                    }
                }
            }
        }

        private void OnFailure(Exception ex)
        {
            lock (_lock)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;
                if (_state == CircuitBreakerState.HalfOpen || _failureCount >= _options.FailureThreshold)
                {
                    _state = CircuitBreakerState.Open;
                }
            }
        }
    }

    public enum CircuitBreakerState { Closed, Open, HalfOpen }
    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
    }
}

// =====================================================================================
// 5. TRUE MULTI-SERVICE gRPC GATEWAY
// =====================================================================================

namespace Instrument.Data.Gateways
{
    /// <summary>
    /// True gRPC Gateway interface supporting multiple services
    /// </summary>
    public interface IGrpcGateway
    {
        /// <summary>
        /// Execute a single operation against any configured gRPC service
        /// </summary>
        Task<GrpcOperationResult<TResult>> ExecuteAsync<TRequest, TResult>(
            GrpcOperation<TRequest, TResult> operation,
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResult : class;

        /// <summary>
        /// Execute multiple requests of the same operation type
        /// </summary>
        Task<BatchGrpcOperationResult<TResult>> ExecuteBatchAsync<TRequest, TResult>(
            BatchGrpcOperation<TRequest, TResult> batchOperation,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResult : class;

        /// <summary>
        /// Test connectivity to a specific service
        /// </summary>
        Task<bool> TestServiceConnectionAsync(string serviceName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get health status of all services
        /// </summary>
        Task<GatewayHealthStatus> GetHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear cache for specific service/operation
        /// </summary>
        Task ClearCacheAsync(string serviceName = null, string operationId = null);
    }

    /// <summary>
    /// Multi-service gRPC Gateway implementation
    /// </summary>
    public class GrpcGateway : IGrpcGateway, IDisposable
    {
        private readonly GrpcGatewayOptions _options;
        private readonly ILogger<GrpcGateway> _logger;
        private readonly IGrpcChannelManager _channelManager;
        private readonly IMemoryCache _cache;
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentDictionary<string, CustomRetryPolicy> _retryPolicies = new();
        private readonly ConcurrentDictionary<string, CircuitBreaker> _circuitBreakers = new();
        private readonly IServiceProvider _serviceProvider;

        public GrpcGateway(
            IOptions<GrpcGatewayOptions> options,
            ILogger<GrpcGateway> logger,
            IGrpcChannelManager channelManager,
            IMemoryCache cache,
            IServiceProvider serviceProvider)
        {
            _options = options.Value;
            _logger = logger;
            _channelManager = channelManager;
            _cache = cache;
            _serviceProvider = serviceProvider;
            _semaphore = new SemaphoreSlim(_options.MaxConcurrentRequests, _options.MaxConcurrentRequests);

            _logger.LogInformation("Multi-service gRPC Gateway initialized for {ServiceCount} services",
                _options.Services.Count);
        }

        public async Task<GrpcOperationResult<TResult>> ExecuteAsync<TRequest, TResult>(
            GrpcOperation<TRequest, TResult> operation,
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResult : class
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new GrpcOperationResult<TResult>
            {
                ServiceName = operation.ServiceName,
                OperationId = operation.OperationId
            };

            _logger.LogDebug("Executing operation {OperationId} on service {ServiceName}",
                operation.OperationId, operation.ServiceName);

            // Validate service is configured
            if (!_options.Services.ContainsKey(operation.ServiceName))
            {
                result.Success = false;
                result.ErrorMessage = $"Service '{operation.ServiceName}' is not configured";
                result.ExecutionTime = stopwatch.Elapsed;
                return result;
            }

            // Check cache first
            if (_options.EnableCaching && operation.IsCacheable)
            {
                var cacheKey = operation.GenerateCacheKey(request);
                if (_cache.TryGetValue(cacheKey, out TResult cachedData))
                {
                    _logger.LogDebug("Cache hit for {ServiceName}.{OperationId}",
                        operation.ServiceName, operation.OperationId);
                    
                    result.Success = true;
                    result.Data = cachedData;
                    result.FromCache = true;
                    result.ExecutionTime = stopwatch.Elapsed;
                    return result;
                }
            }

            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                // Get service-specific resilience components
                var retryPolicy = GetRetryPolicyForService(operation.ServiceName);
                var circuitBreaker = GetCircuitBreakerForService(operation.ServiceName);

                var data = await circuitBreaker.ExecuteAsync(async () =>
                {
                    return await retryPolicy.ExecuteAsync(async ct =>
                    {
                        var channel = _channelManager.GetChannel(operation.ServiceName);
                        
                        // Apply timeout
                        var serviceConfig = _options.Services[operation.ServiceName];
                        var timeout = operation.Timeout ?? 
                                    TimeSpan.FromSeconds(serviceConfig.TimeoutSeconds ?? _options.Defaults.TimeoutSeconds);
                        
                        using var timeoutCts = new CancellationTokenSource(timeout);
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, ct);

                        return await operation.ExecuteAsync(channel, request, linkedCts.Token);
                    }, cancellationToken);
                });

                // Cache successful result
                if (_options.EnableCaching && operation.IsCacheable && data != null)
                {
                    var cacheKey = operation.GenerateCacheKey(request);
                    var cacheExpiry = TimeSpan.FromMinutes(_options.CacheExpiryMinutes);
                    _cache.Set(cacheKey, data, cacheExpiry);
                    
                    // Also store as fallback
                    _cache.Set($"{cacheKey}_fallback", data, cacheExpiry.Multiply(4));
                }

                result.Success = true;
                result.Data = data;
                result.FromCache = false;

                _logger.LogInformation("Successfully executed {ServiceName}.{OperationId} in {ElapsedMs}ms",
                    operation.ServiceName, operation.OperationId, stopwatch.ElapsedMilliseconds);
            }
            catch (CircuitBreakerOpenException)
            {
                _logger.LogWarning("Operation {ServiceName}.{OperationId} failed due to circuit breaker",
                    operation.ServiceName, operation.OperationId);
                
                result.Success = false;
                result.ErrorMessage = "Service temporarily unavailable (circuit breaker open)";

                // Try fallback cache
                if (_options.EnableCaching && operation.IsCacheable)
                {
                    var fallbackKey = $"{operation.GenerateCacheKey(request)}_fallback";
                    if (_cache.TryGetValue(fallbackKey, out TResult fallbackData))
                    {
                        result.Success = true;
                        result.Data = fallbackData;
                        result.FromCache = true;
                        _logger.LogInformation("Returned fallback data for {ServiceName}.{OperationId}",
                            operation.ServiceName, operation.OperationId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Operation {ServiceName}.{OperationId} failed",
                    operation.ServiceName, operation.OperationId);
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                _semaphore.Release();
                result.ExecutionTime = stopwatch.Elapsed;
            }

            return result;
        }

        public async Task<BatchGrpcOperationResult<TResult>> ExecuteBatchAsync<TRequest, TResult>(
            BatchGrpcOperation<TRequest, TResult> batchOperation,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResult : class
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new BatchGrpcOperationResult<TResult>
            {
                BatchId = batchOperation.BatchId,
                ServiceName = batchOperation.Operation.ServiceName,
                OperationId = batchOperation.Operation.OperationId,
                TotalRequests = batchOperation.Requests.Count()
            };

            _logger.LogInformation("Executing batch operation {BatchId} for {ServiceName}.{OperationId} with {RequestCount} requests",
                batchOperation.BatchId, batchOperation.Operation.ServiceName, 
                batchOperation.Operation.OperationId, result.TotalRequests);

            var semaphore = new SemaphoreSlim(batchOperation.MaxConcurrency);
            var tasks = batchOperation.Requests.Select(async request =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    return await ExecuteAsync(batchOperation.Operation, request, cancellationToken);
                }
                catch (Exception ex)
                {
                    if (!batchOperation.ContinueOnError)
                        throw;

                    return new GrpcOperationResult<TResult>
                    {
                        Success = false,
                        ErrorMessage = ex.Message,
                        ServiceName = batchOperation.Operation.ServiceName,
                        OperationId = batchOperation.Operation.OperationId
                    };
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);

            result.Results.AddRange(results);
            result.SuccessfulRequests = results.Count(r => r.Success);
            result.FailedRequests = results.Count(r => !r.Success);
            result.TotalExecutionTime = stopwatch.Elapsed;

            _logger.LogInformation("Batch operation {BatchId} completed: {SuccessCount}/{TotalCount} successful in {ElapsedMs}ms",
                batchOperation.BatchId, result.SuccessfulRequests, result.TotalRequests, stopwatch.ElapsedMilliseconds);

            return result;
        }

        public async Task<bool> TestServiceConnectionAsync(string serviceName, CancellationToken cancellationToken = default)
        {
            return await _channelManager.TestServiceConnectionAsync(serviceName, cancellationToken);
        }

        public async Task<GatewayHealthStatus> GetHealthAsync(CancellationToken cancellationToken = default)
        {
            var serviceHealthStatuses = await _channelManager.GetAllServiceHealthAsync(cancellationToken);
            
            var overallHealth = serviceHealthStatuses.Values.All(s => s.IsConnected) ? "Healthy" :
                               serviceHealthStatuses.Values.Any(s => s.IsConnected) ? "Degraded" : "Unhealthy";

            return new GatewayHealthStatus
            {
                Timestamp = DateTime.UtcNow,
                OverallStatus = overallHealth,
                ServiceStatuses = serviceHealthStatuses,
                CacheEnabled = _options.EnableCaching,
                AvailableConnections = _semaphore.CurrentCount,
                MaxConcurrentConnections = _options.MaxConcurrentRequests
            };
        }

        public Task ClearCacheAsync(string serviceName = null, string operationId = null)
        {
            // This is a simplified version - in production you might want a more sophisticated cache management
            _logger.LogInformation("Cache clear requested for service: {ServiceName}, operation: {OperationId}",
                serviceName ?? "ALL", operationId ?? "ALL");
            
            // Note: IMemoryCache doesn't provide pattern-based clearing
            // You might want to implement a custom cache wrapper for more advanced scenarios
            
            return Task.CompletedTask;
        }

        #region Private Helper Methods

        private CustomRetryPolicy GetRetryPolicyForService(string serviceName)
        {
            return _retryPolicies.GetOrAdd(serviceName, name =>
            {
                var serviceConfig = _options.Services[name];
                var retryOptions = serviceConfig.RetryPolicy ?? _options.RetryPolicy;
                var logger = _serviceProvider.GetRequiredService<ILogger<CustomRetryPolicy>>();
                return new CustomRetryPolicy(retryOptions, logger);
            });
        }

        private CircuitBreaker GetCircuitBreakerForService(string serviceName)
        {
            return _circuitBreakers.GetOrAdd(serviceName, name =>
            {
                var serviceConfig = _options.Services[name];
                var circuitBreakerOptions = serviceConfig.CircuitBreaker ?? _options.CircuitBreaker;
                var logger = _serviceProvider.GetRequiredService<ILogger<CircuitBreaker>>();
                return new CircuitBreaker(circuitBreakerOptions, logger);
            });
        }

        #endregion

        public void Dispose()
        {
            _semaphore?.Dispose();
            _channelManager?.Dispose();
        }
    }

    /// <summary>
    /// Overall gateway health status
    /// </summary>
    public class GatewayHealthStatus
    {
        public DateTime Timestamp { get; set; }
        public string OverallStatus { get; set; }
        public Dictionary<string, ServiceHealthStatus> ServiceStatuses { get; set; } = new();
        public bool CacheEnabled { get; set; }
        public int AvailableConnections { get; set; }
        public int MaxConcurrentConnections { get; set; }
    }
}

// =====================================================================================
// 6. CONCRETE OPERATION EXAMPLES
// =====================================================================================

namespace Instrument.Data.Operations
{
    // Request/Response DTOs
    public class GetSequenceDefinitionsRequest
    {
        public string TechnologyFilter { get; set; }
        public DateTime? ModifiedSince { get; set; }
        public int? MaxResults { get; set; }
    }

    public class GetSequenceByIdRequest
    {
        public int SequenceId { get; set; }
        public bool IncludeParameters { get; set; } = true;
    }

    /// <summary>
    /// Example operation targeting the "SequenceService"
    /// </summary>
    public class GetSequenceDefinitionsOperation : GrpcOperation<GetSequenceDefinitionsRequest, string>
    {
        public override string ServiceName => "SequenceService";
        public override string OperationId => "GetSequenceDefinitions";
        public override bool IsCacheable => true;
        public override TimeSpan? Timeout => TimeSpan.FromSeconds(60);

        public override string GenerateCacheKey(GetSequenceDefinitionsRequest request)
        {
            return $"{ServiceName}_{OperationId}_{request.TechnologyFilter}_{request.ModifiedSince?.ToString("yyyyMMdd")}_{request.MaxResults}";
        }

        public override async Task<string> ExecuteAsync(GrpcChannel channel, GetSequenceDefinitionsRequest request, CancellationToken cancellationToken)
        {
            // Implement based on your .proto definitions
            // Example:
            // var client = new SequenceService.SequenceServiceClient(channel);
            // var grpcRequest = new GetSequenceDefinitionsGrpcRequest
            // {
            //     TechnologyFilter = request.TechnologyFilter ?? "",
            //     ModifiedSince = request.ModifiedSince?.ToTimestamp(),
            //     MaxResults = request.MaxResults ?? 1000
            // };
            // 
            // var response = await client.GetSequenceDefinitionsAsync(grpcRequest, cancellationToken: cancellationToken);
            // return ConvertSequencesToJsonString(response.Sequences);

            throw new NotImplementedException("Implement based on your .proto file");
        }
    }

    /// <summary>
    /// Example operation targeting the "ParameterService"
    /// </summary>
    public class GetParameterDefinitionsOperation : GrpcOperation<GetSequenceDefinitionsRequest, string>
    {
        public override string ServiceName => "ParameterService";  // Different service!
        public override string OperationId => "GetParameterDefinitions";

        public override async Task<string> ExecuteAsync(GrpcChannel channel, GetSequenceDefinitionsRequest request, CancellationToken cancellationToken)
        {
            // Different service, different client
            // var client = new ParameterService.ParameterServiceClient(channel);
            throw new NotImplementedException("Implement based on your .proto file");
        }
    }
}

// =====================================================================================
// 7. CONFIGURATION EXAMPLE
// =====================================================================================

/*
{
  "GrpcGateway": {
    "Defaults": {
      "UseSecureConnection": true,
      "TimeoutSeconds": 30,
      "MaxConnectionsPerService": 5
    },
    "MaxConcurrentRequests": 20,
    "EnableCaching": true,
    "CacheExpiryMinutes": 15,
    "RetryPolicy": {
      "MaxRetries": 3,
      "InitialDelayMs": 1000,
      "BackoffMultiplier": 2.0,
      "UseJitter": true
    },
    "CircuitBreaker": {
      "FailureThreshold": 5,
      "OpenToHalfOpenTimeoutSeconds": 30
    },
    "Services": {
      "SequenceService": {
        "BaseAddress": "https://sequence-service.com:5001",
        "Endpoints": [
          "https://sequence-service-1.com:5001",
          "https://sequence-service-2.com:5001"
        ],
        "TimeoutSeconds": 60
      },
      "ParameterService": {
        "BaseAddress": "https://parameter-service.com:5002",
        "UseSecureConnection": false,
        "RetryPolicy": {
          "MaxRetries": 5,
          "InitialDelayMs": 500
        }
      },
      "ResourceService": {
        "BaseAddress": "https://resource-service.com:5003"
      }
    }
  }
}
*/

// =====================================================================================
// 8. USAGE EXAMPLES
// =====================================================================================

namespace Instrument.Data.Examples
{
    /// <summary>
    /// Example showing how to use the multi-service gateway
    /// </summary>
    public class MultiServiceUsageExample
    {
        private readonly IGrpcGateway _gateway;

        public MultiServiceUsageExample(IGrpcGateway gateway)
        {
            _gateway = gateway;
        }

        /// <summary>
        /// Call different services through the same gateway interface
        /// </summary>
        public async Task CallMultipleServices()
        {
            // Call sequence service
            var sequenceRequest = new GetSequenceDefinitionsRequest
            {
                TechnologyFilter = "TechA",
                MaxResults = 100
            };

            var sequenceResult = await _gateway.ExecuteAsync(
                new GetSequenceDefinitionsOperation(),
                sequenceRequest);

            if (sequenceResult.Success)
            {
                // Process sequence data
                Console.WriteLine($"Got sequences from {sequenceResult.ServiceName}: {sequenceResult.Data?.Length} chars");
            }

            // Call parameter service (different service, same gateway)
            var parameterResult = await _gateway.ExecuteAsync(
                new GetParameterDefinitionsOperation(),
                sequenceRequest); // Same request DTO, different service

            if (parameterResult.Success)
            {
                // Process parameter data
                Console.WriteLine($"Got parameters from {parameterResult.ServiceName}: {parameterResult.Data?.Length} chars");
            }

            // Test all service connections
            foreach (var serviceName in new[] { "SequenceService", "ParameterService", "ResourceService" })
            {
                var isConnected = await _gateway.TestServiceConnectionAsync(serviceName);
                Console.WriteLine($"{serviceName}: {(isConnected ? "Connected" : "Disconnected")}");
            }
        }

        /// <summary>
        /// Batch operations across services
        /// </summary>
        public async Task BatchOperationsExample()
        {
            var requests = new[]
            {
                new GetSequenceDefinitionsRequest { TechnologyFilter = "TechA" },
                new GetSequenceDefinitionsRequest { TechnologyFilter = "TechB" },
                new GetSequenceDefinitionsRequest { TechnologyFilter = "TechC" }
            };

            var batchOperation = new BatchGrpcOperation<GetSequenceDefinitionsRequest, string>
            {
                Operation = new GetSequenceDefinitionsOperation(),
                Requests = requests,
                MaxConcurrency = 3,
                ContinueOnError = true
            };

            var batchResult = await _gateway.ExecuteBatchAsync(batchOperation);

            Console.WriteLine($"Batch {batchResult.BatchId}: {batchResult.SuccessfulRequests}/{batchResult.TotalRequests} successful");
        }
    }
}