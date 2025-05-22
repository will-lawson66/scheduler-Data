using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Instrument.Data.Adapters.Grpc;

/// <summary>
/// Base class for gRPC clients with common functionality
/// </summary>
public abstract class BaseGrpcClient
{
    protected readonly ILogger Logger;
    protected readonly GrpcAdapterOptions Options;
    protected readonly string ServiceName;
    
    /// <summary>
    /// Creates a new base gRPC client
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="options">gRPC adapter options</param>
    /// <param name="serviceName">Service name</param>
    protected BaseGrpcClient(
        ILogger logger,
        IOptions<GrpcAdapterOptions> options,
        string serviceName)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
    }
    
    /// <summary>
    /// Creates a gRPC channel for the service
    /// </summary>
    /// <returns>gRPC channel</returns>
    protected GrpcChannel CreateChannel()
    {
        var channelOptions = new GrpcChannelOptions
        {
            MaxReceiveMessageSize = null, // Unlimited
            MaxSendMessageSize = null,    // Unlimited
        };
        
        if (!Options.UseSecureConnection)
        {
            Logger.LogWarning("Using insecure gRPC connection for service: {ServiceName}", ServiceName);
            
            var httpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = 
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            
            channelOptions.HttpHandler = httpHandler;
        }
        
        var address = Options.BaseAddress;
        Logger.LogDebug("Creating gRPC channel for service {ServiceName} at address: {Address}", 
            ServiceName, address);
            
        return GrpcChannel.ForAddress(address, channelOptions);
    }
    
    /// <summary>
    /// Executes a gRPC function with retry logic
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="func">Function to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Function result</returns>
    protected async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> func,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        
        for (int attempt = 1; attempt <= Options.RetryCount + 1; attempt++)
        {
            try
            {
                // If it's a retry, add some delay
                if (attempt > 1)
                {
                    int delayMs = Options.RetryDelayMilliseconds * (attempt - 1);
                    Logger.LogDebug("Retry attempt {Attempt} for {ServiceName} after {DelayMs}ms", 
                        attempt, ServiceName, delayMs);
                    await Task.Delay(delayMs, cancellationToken);
                }
                
                // Set timeout for the operation
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Options.TimeoutSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);
                
                return await func(linkedCts.Token);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
            {
                lastException = ex;
                Logger.LogWarning(ex, "Timeout occurred on attempt {Attempt} for {ServiceName}", 
                    attempt, ServiceName);
            }
            catch (RpcException ex) when (
                ex.StatusCode == StatusCode.Unavailable || 
                ex.StatusCode == StatusCode.Internal ||
                ex.StatusCode == StatusCode.Unknown)
            {
                lastException = ex;
                Logger.LogWarning(ex, "Transient error on attempt {Attempt} for {ServiceName}, Status: {Status}", 
                    attempt, ServiceName, ex.Status);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                // Operation was canceled by the caller, don't retry
                Logger.LogWarning(ex, "Operation canceled for {ServiceName}", ServiceName);
                throw;
            }
            catch (Exception ex)
            {
                // Non-transient error, don't retry
                Logger.LogError(ex, "Non-transient error occurred for {ServiceName}", ServiceName);
                throw;
            }
        }
        
        Logger.LogError(lastException, "All retry attempts failed for {ServiceName}", ServiceName);
        throw lastException ?? new InvalidOperationException($"All retry attempts failed for {ServiceName}");
    }
}