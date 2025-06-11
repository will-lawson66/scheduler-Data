namespace Instrument.Data.Grpc;
using System;
using System.Threading.Tasks;

/// <summary>
/// Generic bound gRPC operation
/// </summary>
public class GrpcOperation<TRequest, TResponse> : IGrpcOperation<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    private readonly Func<TRequest, Task<TResponse>> _serviceMethod;

    public GrpcOperation(
        string serviceName,
        string operationName,
        TimeSpan timeout,
        Func<TRequest, Task<TResponse>> serviceMethod
        )
    {
        ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
        Timeout = timeout;
        _serviceMethod = serviceMethod ?? throw new ArgumentNullException(nameof(serviceMethod));
    }

    public string ServiceName { get; }
    public string OperationName { get; }
    public TimeSpan? Timeout { get; }

    public Task<TResponse> ExecuteAsync(TRequest request)
    {
        return _serviceMethod(request);
    }
}
