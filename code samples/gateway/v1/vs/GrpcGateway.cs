namespace Instrument.Scheduling.Data.Grpc;

using global::Grpc.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

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
