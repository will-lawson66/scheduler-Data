namespace Instrument.Data.Adapters;

/// <summary>
/// Interface for gRPC data import adapter
/// </summary>
public interface IGrpcDataAdapter
{
    /// <summary>
    /// Imports all data from gRPC services to the database
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    Task ImportAllDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports sequence data from gRPC service to the database
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    Task ImportSequencesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports parameter data from gRPC service to the database
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    Task ImportParametersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports resource data from gRPC service to the database
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    Task ImportResourcesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports sequence group data from gRPC service to the database
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    Task ImportSequenceGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to all gRPC services
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Dictionary with service names and connection status</returns>
    Task<Dictionary<string, bool>> TestConnectionsAsync(CancellationToken cancellationToken = default);
}