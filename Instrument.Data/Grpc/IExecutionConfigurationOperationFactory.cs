namespace Instrument.Data.Grpc;
using Instrument.Execution.Grpc.Configuration;

/// <summary>
/// Abstract factory specifically for ExecutionConfigurationService operations
/// </summary>
public interface IExecutionConfigurationOperationFactory : IGrpcOperationFactory
{
    /// <summary>
    /// Create operation to get current configuration
    /// </summary>
    IGrpcOperation<FetchExecutionConfigurationRequest, FetchExecutionConfigurationResponse>
        CreateGetCurrentConfigurationOperation();

    /// <summary>
    /// Create operation to get sequence configuration
    /// </summary>
    IGrpcOperation<FetchSequenceConfigurationRequest, FetchSequenceConfigurationResponse>
        CreateGetSequenceConfigurationOperation();

    /// <summary>
    /// Create operation to get resource configuration
    /// </summary>
    IGrpcOperation<FetchResourceConfigurationRequest, FetchResourceConfigurationResponse>
        CreateGetResourceConfigurationOperation();
}
