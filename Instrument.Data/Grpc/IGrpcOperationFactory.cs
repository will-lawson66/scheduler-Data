namespace Instrument.Data.Grpc;
using System;
using System.Threading.Tasks;

/// <summary>
/// Abstract factory for creating gRPC operations for a specific service
/// </summary>
public interface IGrpcOperationFactory
{
    /// <summary>
    /// The name of the service this factory creates operations for
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// Request timeout to be set in the concrete operation factory.
    /// This property overrides the default timeout from gateway configuration.
    /// </summary>
    TimeSpan Timeout { get; }

    /// <summary>
    /// Create a custom operation with the provided service method
    /// </summary>
    IGrpcOperation<TRequest, TResponse> CreateOperation<TRequest, TResponse>(
        string operationName,
        Func<TRequest, Task<TResponse>> executor)
        where TRequest : class
        where TResponse : class;
}
