using Instrument.Data.Entities;

namespace Instrument.Data.Adapters.Grpc;

/// <summary>
/// Interface for sequence gRPC client
/// </summary>
public interface ISequenceGrpcClient
{
    /// <summary>
    /// Gets all sequences from the gRPC service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IEnumerable<Sequence>> GetAllSequencesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a sequence by ID from the gRPC service
    /// </summary>
    /// <param name="id">Sequence ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<Sequence?> GetSequenceByIdAsync(int id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tests connection to the service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for parameter gRPC client
/// </summary>
public interface IParameterGrpcClient
{
    /// <summary>
    /// Gets all parameters from the gRPC service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IEnumerable<Parameter>> GetAllParametersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets parameters for a sequence from the gRPC service
    /// </summary>
    /// <param name="sequenceId">Sequence ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IEnumerable<SequenceParameter>> GetParametersForSequenceAsync(int sequenceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tests connection to the service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for resource gRPC client
/// </summary>
public interface IResourceGrpcClient
{
    /// <summary>
    /// Gets all resources from the gRPC service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IEnumerable<Resource>> GetAllResourcesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a resource by ID from the gRPC service
    /// </summary>
    /// <param name="id">Resource ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<Resource?> GetResourceByIdAsync(int id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tests connection to the service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for sequence group gRPC client
/// </summary>
public interface ISequenceGroupGrpcClient
{
    /// <summary>
    /// Gets all sequence groups from the gRPC service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IEnumerable<SequenceGroup>> GetAllSequenceGroupsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets sequence group sequences from the gRPC service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IEnumerable<SequenceGroupSequence>> GetSequenceGroupSequencesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tests connection to the service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}