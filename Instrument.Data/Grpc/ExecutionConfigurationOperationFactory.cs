namespace Instrument.Data.Grpc;
using System;
using System.Threading.Tasks;
using Instrument.Execution.Grpc.Configuration;

/// <summary>
/// Concrete factory for ExecutionConfigurationService operations
/// </summary>
public class ExecutionConfigurationOperationFactory : IExecutionConfigurationOperationFactory
{
    private readonly IExecutionConfigurationService _service;

    public ExecutionConfigurationOperationFactory(IExecutionConfigurationService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <inheritdoc />
    public string ServiceName => "ExecutionConfigurationService";

    /// <inheritdoc />
    public TimeSpan Timeout => TimeSpan.FromMinutes(2);

    public IGrpcOperation<TRequest, TResponse> CreateOperation<TRequest, TResponse>(
        string operationName,
        Func<TRequest, Task<TResponse>> serviceMethod)
        where TRequest : class
        where TResponse : class
    {
        return new GrpcOperation<TRequest, TResponse>(ServiceName, operationName, Timeout, serviceMethod);
    }

    public IGrpcOperation<FetchExecutionConfigurationRequest, FetchExecutionConfigurationResponse>
        CreateGetCurrentConfigurationOperation()
    {
        return CreateOperation<FetchExecutionConfigurationRequest, FetchExecutionConfigurationResponse>(
            "GetCurrentConfiguration",
            _service.GetCurrentConfigurationAsync);
    }

    public IGrpcOperation<FetchSequenceConfigurationRequest, FetchSequenceConfigurationResponse>
        CreateGetSequenceConfigurationOperation()
    {
        return CreateOperation<FetchSequenceConfigurationRequest, FetchSequenceConfigurationResponse>(
            "GetSequenceConfiguration",
            _service.GetSequenceConfigurationAsync);
    }

    public IGrpcOperation<FetchResourceConfigurationRequest, FetchResourceConfigurationResponse>
        CreateGetResourceConfigurationOperation()
    {
        return CreateOperation<FetchResourceConfigurationRequest, FetchResourceConfigurationResponse>(
            "GetResourceConfiguration",
            _service.GetResourceConfigurationAsync);
    }
}
