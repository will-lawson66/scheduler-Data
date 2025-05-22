using Grpc.Core;
using Instrument.Data.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Protocol.Sequences;

namespace Instrument.Data.Adapters.Grpc;

/// <summary>
/// gRPC client for sequence service
/// </summary>
public class SequenceGrpcClient : BaseGrpcClient, ISequenceGrpcClient
{
    /// <summary>
    /// Creates a new sequence gRPC client
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="options">gRPC adapter options</param>
    public SequenceGrpcClient(
        ILogger<SequenceGrpcClient> logger,
        IOptions<GrpcAdapterOptions> options)
        : base(logger, options, "SequenceService")
    {
    }
    
    /// <inheritdoc />
    public async Task<IEnumerable<Sequence>> GetAllSequencesAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Getting all sequences from gRPC service");
        
        return await ExecuteWithRetryAsync(async (token) =>
        {
            using var channel = CreateChannel();
            var client = new SequenceService.SequenceServiceClient(channel);
            
            var request = new GetAllSequencesRequest();
            var response = await client.GetAllSequencesAsync(request, 
                deadline: DateTime.UtcNow.AddSeconds(Options.TimeoutSeconds),
                cancellationToken: token);
                
            // Map from gRPC response to domain entities
            var sequences = response.Sequences.Select(s => new Sequence
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Created = s.Created.ToDateTime(),
                Updated = s.Updated.ToDateTime(),
                IsActive = s.IsActive
            }).ToList();
            
            Logger.LogInformation("Retrieved {Count} sequences from gRPC service", sequences.Count);
            return sequences;
        }, cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task<Sequence?> GetSequenceByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Getting sequence with ID {Id} from gRPC service", id);
        
        return await ExecuteWithRetryAsync(async (token) =>
        {
            using var channel = CreateChannel();
            var client = new SequenceService.SequenceServiceClient(channel);
            
            var request = new GetSequenceByIdRequest { Id = id };
            
            try
            {
                var response = await client.GetSequenceByIdAsync(request,
                    deadline: DateTime.UtcNow.AddSeconds(Options.TimeoutSeconds),
                    cancellationToken: token);
                    
                // Map from gRPC response to domain entity
                return new Sequence
                {
                    Id = response.Sequence.Id,
                    Name = response.Sequence.Name,
                    Description = response.Sequence.Description,
                    Created = response.Sequence.Created.ToDateTime(),
                    Updated = response.Sequence.Updated.ToDateTime(),
                    IsActive = response.Sequence.IsActive
                };
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                Logger.LogWarning("Sequence with ID {Id} not found in gRPC service", id);
                return null;
            }
        }, cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Testing connection to sequence gRPC service");
        
        try
        {
            await ExecuteWithRetryAsync(async (token) =>
            {
                using var channel = CreateChannel();
                var client = new SequenceService.SequenceServiceClient(channel);
                
                var request = new PingRequest();
                var response = await client.PingAsync(request,
                    deadline: DateTime.UtcNow.AddSeconds(Options.TimeoutSeconds),
                    cancellationToken: token);
                    
                return response.Status;
            }, cancellationToken);
            
            Logger.LogInformation("Successfully connected to sequence gRPC service");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to connect to sequence gRPC service");
            return false;
        }
    }
}

/// <summary>
/// Extension method to convert from Google.Protobuf.WellKnownTypes.Timestamp to DateTime
/// </summary>
public static class TimestampExtensions
{
    public static DateTime ToDateTime(this Google.Protobuf.WellKnownTypes.Timestamp timestamp)
    {
        return timestamp.ToDateTime().ToLocalTime();
    }
}