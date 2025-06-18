namespace Instrument.Data.Grpc;
using System;
using System.Threading;
using System.Threading.Tasks;
using global::Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Exponential backoff retry policy
/// </summary>
public class ExponentialBackoffRetryPolicy : IRetryPolicy
{
    private readonly RetryOptions _options;
    private readonly ILogger<ExponentialBackoffRetryPolicy> _logger;

    public ExponentialBackoffRetryPolicy(
        IOptions<GrpcGatewayOptions> options,
        ILogger<ExponentialBackoffRetryPolicy> logger)
    {
        _options = options.Value.RetryOptions;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= _options.MaxAttempts; attempt++)
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
            catch (Exception ex) when (IsRetriable(ex) && attempt < _options.MaxAttempts)
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

    private static bool IsRetriable(Exception ex) => ex switch
    {
        RpcException rpc => rpc.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded,
        TaskCanceledException => true,
        TimeoutException => true,
        _ => false
    };
}
