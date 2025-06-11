namespace Instrument.Data.Grpc;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Thread-safe multi-service gRPC Gateway
/// </summary>
public class GrpcGateway : IGrpcGateway, IDisposable
{
    private readonly IRetryPolicy _retryPolicy;
    private readonly ILogger<GrpcGateway> _logger;
    private readonly GrpcGatewayOptions _options;
    private readonly SemaphoreSlim _semaphore;
    private readonly GatewayStatistics _statistics;
    private volatile bool _disposed;


    public GrpcGateway(
        IRetryPolicy retryPolicy,
        ILogger<GrpcGateway> logger,
        IOptions<GrpcGatewayOptions> options,
        IExecutionConfigurationOperationFactory executionConfigurationOperationFactory
        )
    {
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _semaphore = new SemaphoreSlim(_options.MaxConcurrentRequests, _options.MaxConcurrentRequests);
        _statistics = new GatewayStatistics();
        ExecutionConfigurationOperations = executionConfigurationOperationFactory ?? throw new ArgumentNullException(nameof(executionConfigurationOperationFactory));
    }

    public IExecutionConfigurationOperationFactory ExecutionConfigurationOperations { get; }

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
                // Apply timeout: operation > default
                var timeout = GetTimeout(operation.Timeout);
                using var timeoutCts = new CancellationTokenSource(timeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, ct);

                return await operation.ExecuteAsync(request);
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
            // Placeholder for actual health check
            await Task.Delay(1, cancellationToken); 
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


    private TimeSpan GetTimeout(TimeSpan? operationTimeout)
    {
        // Priority: operation timeout > configuration timeout
        if (operationTimeout.HasValue)
        {
            return operationTimeout.Value;
        }

        return TimeSpan.FromSeconds(_options.DefaultTimeoutSeconds);
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed)
        {
            return;
        }

        throw new ObjectDisposedException(nameof(GrpcGateway));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _semaphore?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
