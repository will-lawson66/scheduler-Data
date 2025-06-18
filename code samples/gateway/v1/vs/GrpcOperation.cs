namespace Instrument.Scheduling.Data.Grpc;

using System;
using System.Threading.Tasks;
using System.Threading;

/// <summary>
/// Generic gRPC operation with explicit request and result types.
/// </summary>
/// <typeparam name="TRequest">A gRPC request</typeparam>
/// <typeparam name="TResult">A gRPC response</typeparam>
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
    public virtual TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Execute the gRPC operation
    /// </summary>
    /// <param name="client">gRPC channel to the target service</param>
    /// <param name="request">Request data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    public abstract Task<TResult> ExecuteAsync(object client, TRequest request, CancellationToken cancellationToken);
}
