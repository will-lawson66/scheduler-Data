namespace Instrument.Scheduling.Data.Grpc;

using System;
using System.Threading;
using System.Threading.Tasks;
using global::Grpc.Core;
using Microsoft.Extensions.Logging;

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
        Exception? lastException = null;
        for (var attempt = 0; attempt <= _options.MaxRetries; attempt++)
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
            catch (Exception? ex) when (IsRetriableException(ex) && attempt < _options.MaxRetries)
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

    private static bool IsRetriableException(Exception? ex)
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

