namespace Instrument.Execution.Grpc.Configuration;

using System.Threading.Tasks;
using ProtoBuf.Grpc.Configuration;


/// <summary>
/// A service for fetching and updating the Execution Engine's
/// configuration. Includes sequences, period information, and
/// resources. 
/// </summary>
[Service]
public interface IExecutionConfigurationService
{
    /// <summary>
    /// Fetches the current configuration. 
    /// </summary>
    /// <param name="request">
    /// The request that contains details associated with the query.
    /// </param>
    /// <returns>
    /// A response that should contain either the configuration or a
    /// list of errors. 
    /// </returns>
    Task<FetchExecutionConfigurationResponse> GetCurrentConfigurationAsync(FetchExecutionConfigurationRequest request);

    /// <summary>
    /// Fetches a specific sequence by its key.
    /// </summary>
    /// <param name="request">
    /// Contains a sequence key to identify the sequence to fetch.
    /// </param>
    /// <returns>
    /// The sequence with a request id. 
    /// </returns>
    Task<FetchSequenceConfigurationResponse> GetSequenceConfigurationAsync(FetchSequenceConfigurationRequest request);

    /// <summary>
    /// Fetches a specific resource by its key.
    /// </summary>
    /// <param name="request">
    /// Contains a resource key to identify the resource to fetch.
    /// </param>
    /// <returns>
    /// The resource with a request id. 
    /// </returns>
    Task<FetchResourceConfigurationResponse> GetResourceConfigurationAsync(FetchResourceConfigurationRequest request);

    /// <summary>
    /// Reloads the current configuration in order to catch any new changes. 
    /// <returns>
    /// A response that will contain errors if the request ran into any problems.
    /// </returns>
    Task<UpdateExecutionConfigurationResponse> ReloadExecutionConfigurationAsync();
}
