using Instrument.Data.Adapters;
using Microsoft.Extensions.Logging;

namespace Instrument.Data.Api;

/// <summary>
/// API for importing data from gRPC services
/// </summary>
public class GrpcDataImportApi : IDataImportApi
{
    private readonly IGrpcDataAdapter _grpcAdapter;
    private readonly ILogger<GrpcDataImportApi> _logger;
    
    /// <summary>
    /// Creates a new gRPC data import API
    /// </summary>
    /// <param name="grpcAdapter">gRPC data adapter</param>
    /// <param name="logger">Logger</param>
    public GrpcDataImportApi(
        IGrpcDataAdapter grpcAdapter,
        ILogger<GrpcDataImportApi> logger)
    {
        _grpcAdapter = grpcAdapter ?? throw new ArgumentNullException(nameof(grpcAdapter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <inheritdoc />
    public async Task ImportAllDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Importing all data from gRPC services");
        
        try
        {
            await _grpcAdapter.ImportAllDataAsync(cancellationToken);
            _logger.LogInformation("Successfully imported all data from gRPC services");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing data from gRPC services");
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task ImportSequencesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Importing sequences from gRPC services");
        
        try
        {
            await _grpcAdapter.ImportSequencesAsync(cancellationToken);
            _logger.LogInformation("Successfully imported sequences from gRPC services");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing sequences from gRPC services");
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task ImportParametersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Importing parameters from gRPC services");
        
        try
        {
            await _grpcAdapter.ImportParametersAsync(cancellationToken);
            _logger.LogInformation("Successfully imported parameters from gRPC services");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing parameters from gRPC services");
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task ImportResourcesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Importing resources from gRPC services");
        
        try
        {
            await _grpcAdapter.ImportResourcesAsync(cancellationToken);
            _logger.LogInformation("Successfully imported resources from gRPC services");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing resources from gRPC services");
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task ImportSequenceGroupsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Importing sequence groups from gRPC services");
        
        try
        {
            await _grpcAdapter.ImportSequenceGroupsAsync(cancellationToken);
            _logger.LogInformation("Successfully imported sequence groups from gRPC services");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing sequence groups from gRPC services");
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task<Dictionary<string, bool>> TestConnectionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Testing connections to gRPC services");
        
        try
        {
            var results = await _grpcAdapter.TestConnectionsAsync(cancellationToken);
            
            foreach (var result in results)
            {
                _logger.LogInformation("Connection to {ServiceName}: {Status}", 
                    result.Key, result.Value ? "Success" : "Failed");
            }
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connections to gRPC services");
            throw;
        }
    }
}