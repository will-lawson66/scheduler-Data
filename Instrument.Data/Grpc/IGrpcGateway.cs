namespace Instrument.Data.Grpc;
using System.Threading;
using System.Threading.Tasks;


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

    /// <summary>
    /// Get current gateway statistics
    /// </summary>
    GatewayStatistics GetStatistics();

    /// <summary>
    /// Factory with which to create gRPC operations.
    /// </summary>
    IExecutionConfigurationOperationFactory ExecutionConfigurationOperations { get; }
}
