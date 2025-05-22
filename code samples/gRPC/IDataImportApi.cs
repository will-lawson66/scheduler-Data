namespace Instrument.Data.Api;

/// <summary>
/// Interface for data import API
/// </summary>
public interface IDataImportApi
{
    /// <summary>
    /// Imports all data
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    Task ImportAllDataAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Imports sequences
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    Task ImportSequencesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Imports parameters
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    Task ImportParametersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Imports resources
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    Task ImportResourcesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Imports sequence groups
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    Task ImportSequenceGroupsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tests connections to all data sources
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Dictionary with service names and connection status</returns>
    Task<Dictionary<string, bool>> TestConnectionsAsync(CancellationToken cancellationToken = default);
}