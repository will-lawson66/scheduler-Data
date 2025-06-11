namespace Instrument.Scheduling.Data.Grpc;

using System.Threading;
using System.Threading.Tasks;

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
    Task ClearCacheAsync(string? serviceName = null, string? operationId = null);
}
