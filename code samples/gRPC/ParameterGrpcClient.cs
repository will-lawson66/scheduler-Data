using Grpc.Core;
using Instrument.Data.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Protocol.Parameters;

namespace Instrument.Data.Adapters.Grpc;

/// <summary>
/// gRPC client for parameter service
/// </summary>
public class ParameterGrpcClient : BaseGrpcClient, IParameterGrpcClient
{
    /// <summary>
    /// Creates a new parameter gRPC client
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="options">gRPC adapter options</param>
    public ParameterGrpcClient(
        ILogger<ParameterGrpcClient> logger,
        IOptions<GrpcAdapterOptions> options)
        : base(logger, options, "ParameterService")
    {
    }
    
    /// <inheritdoc />
    public async Task<IEnumerable<Parameter>> GetAllParametersAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Getting all parameters from gRPC service");
        
        return await ExecuteWithRetryAsync(async (token) =>
        {
            using var channel = CreateChannel();
            var client = new ParameterService.ParameterServiceClient(channel);
            
            var request = new GetAllParametersRequest();
            var response = await client.GetAllParametersAsync(request, 
                deadline: DateTime.UtcNow.AddSeconds(Options.TimeoutSeconds),
                cancellationToken: token);
                
            // Map from gRPC response to domain entities
            var parameters = response.Parameters.Select(p => new Parameter
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                DataType = p.DataType,
                DefaultValue = p.DefaultValue,
                Created = p.Created.ToDateTime(),
                Updated = p.Updated.ToDateTime()
            }).ToList();
            
            Logger.LogInformation("Retrieved {Count} parameters from gRPC service", parameters.Count);
            return parameters;
        }, cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task<IEnumerable<SequenceParameter>> GetParametersForSequenceAsync(
        int sequenceId, 
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Getting parameters for sequence {SequenceId} from gRPC service", sequenceId);
        
        return await ExecuteWithRetryAsync(async (token) =>
        {
            using var channel = CreateChannel();
            var client = new ParameterService.ParameterServiceClient(channel);
            
            var request = new GetParametersForSequenceRequest { SequenceId = sequenceId };
            var response = await client.GetParametersForSequenceAsync(request,
                deadline: DateTime.UtcNow.AddSeconds(Options.TimeoutSeconds),
                cancellationToken: token);
                
            // Map from gRPC response to domain entities
            var sequenceParameters = response.SequenceParameters.Select(sp => new SequenceParameter
            {
                SequenceId = sp.SequenceId,
                ParameterId = sp.ParameterId,
                OrderNumber = sp.OrderNumber
            }).ToList();
            
            Logger.LogInformation("Retrieved {Count} parameters for sequence {SequenceId} from gRPC service", 
                sequenceParameters.Count, sequenceId);
                
            return sequenceParameters;
        }, cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Testing connection to parameter gRPC service");
        
        try
        {
            await ExecuteWithRetryAsync(async (token) =>
            {
                using var channel = CreateChannel();
                var client = new ParameterService.ParameterServiceClient(channel);
                
                var request = new PingRequest();
                var response = await client.PingAsync(request,
                    deadline: DateTime.UtcNow.AddSeconds(Options.TimeoutSeconds),
                    cancellationToken: token);
                    
                return response.Status;
            }, cancellationToken);
            
            Logger.LogInformation("Successfully connected to parameter gRPC service");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to connect to parameter gRPC service");
            return false;
        }
    }
}