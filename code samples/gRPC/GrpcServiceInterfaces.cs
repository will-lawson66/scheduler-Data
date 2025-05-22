using Instrument.Data.Entities;

namespace Instrument.Data.Services;

/// <summary>
/// Interface for a gRPC service client that fetches sequences
/// </summary>
public interface ISequenceGrpcService
{
    /// <summary>
    /// Gets all sequences from the gRPC service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of sequences</returns>
    Task<IEnumerable<Sequence>> GetAllSequencesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a sequence by ID from the gRPC service
    /// </summary>
    /// <param name="id">Sequence ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sequence if found, otherwise null</returns>
    Task<Sequence?> GetSequenceByIdAsync(int id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tests the connection to the gRPC service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connected, otherwise false</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for a gRPC service client that fetches parameters
/// </summary>
public interface IParameterGrpcService
{
    /// <summary>
    /// Gets all parameters from the gRPC service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of parameters</returns>
    Task<IEnumerable<Parameter>> GetAllParametersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets parameters for a specific sequence from the gRPC service
    /// </summary>
    /// <param name="sequenceId">Sequence ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of parameters</returns>
    Task<IEnumerable<SequenceParameter>> GetParametersForSequenceAsync(int sequenceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tests the connection to the gRPC service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connected, otherwise false</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for a gRPC service client that fetches resources
/// </summary>
public interface IResourceGrpcService
{
    /// <summary>
    /// Gets all resources from the gRPC service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of resources</returns>
    Task<IEnumerable<Resource>> GetAllResourcesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a resource by ID from the gRPC service
    /// </summary>
    /// <param name="id">Resource ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resource if found, otherwise null</returns>
    Task<Resource?> GetResourceByIdAsync(int id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tests the connection to the gRPC service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connected, otherwise false</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for a gRPC service client that fetches sequence groups
/// </summary>
public interface ISequenceGroupGrpcService
{
    /// <summary>
    /// Gets all sequence groups from the gRPC service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of sequence groups</returns>
    Task<IEnumerable<SequenceGroup>> GetAllSequenceGroupsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets sequence to group mappings from the gRPC service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of sequence group sequences</returns>
    Task<IEnumerable<SequenceGroupSequence>> GetSequenceGroupSequencesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tests the connection to the gRPC service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connected, otherwise false</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}