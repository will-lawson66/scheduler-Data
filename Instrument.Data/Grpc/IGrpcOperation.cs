namespace Instrument.Data.Grpc;
using System;
using System.Threading.Tasks;

/// <summary>
/// Represents a gRPC operation that can be executed
/// </summary>
public interface IGrpcOperation<in TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    string ServiceName { get; }
    string OperationName { get; }
    TimeSpan? Timeout { get; }

    /// <summary>
    /// Execute the operation with the provided gRPC client
    /// </summary>
    Task<TResponse> ExecuteAsync(TRequest request);
}

