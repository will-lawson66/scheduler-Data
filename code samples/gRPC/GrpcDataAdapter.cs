using Instrument.Data.Adapters.Grpc;
using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Instrument.Data.Adapters;

/// <summary>
/// Adapter for importing data from gRPC services
/// </summary>
public class GrpcDataAdapter : IGrpcDataAdapter
{
    private readonly SchedulerDbContext _dbContext;
    private readonly ILogger<GrpcDataAdapter> _logger;
    private readonly GrpcAdapterOptions _options;
    
    // Domain services
    private readonly ISequenceService _sequenceService;
    private readonly IParameterService _parameterService;
    private readonly IResourceService _resourceService;
    private readonly ISequenceGroupService _sequenceGroupService;
    
    // gRPC clients
    private readonly ISequenceGrpcClient _sequenceGrpcClient;
    private readonly IParameterGrpcClient _parameterGrpcClient;
    private readonly IResourceGrpcClient _resourceGrpcClient;
    private readonly ISequenceGroupGrpcClient _sequenceGroupGrpcClient;
    
    /// <summary>
    /// Creates a new gRPC data adapter
    /// </summary>
    public GrpcDataAdapter(
        SchedulerDbContext dbContext,
        ILogger<GrpcDataAdapter> logger,
        IOptions<GrpcAdapterOptions> options,
        // Domain services
        ISequenceService sequenceService,
        IParameterService parameterService,
        IResourceService resourceService,
        ISequenceGroupService sequenceGroupService,
        // gRPC clients
        ISequenceGrpcClient sequenceGrpcClient,
        IParameterGrpcClient parameterGrpcClient,
        IResourceGrpcClient resourceGrpcClient,
        ISequenceGroupGrpcClient sequenceGroupGrpcClient)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        
        // Domain services
        _sequenceService = sequenceService ?? throw new ArgumentNullException(nameof(sequenceService));
        _parameterService = parameterService ?? throw new ArgumentNullException(nameof(parameterService));
        _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
        _sequenceGroupService = sequenceGroupService ?? throw new ArgumentNullException(nameof(sequenceGroupService));
        
        // gRPC clients
        _sequenceGrpcClient = sequenceGrpcClient ?? throw new ArgumentNullException(nameof(sequenceGrpcClient));
        _parameterGrpcClient = parameterGrpcClient ?? throw new ArgumentNullException(nameof(parameterGrpcClient));
        _resourceGrpcClient = resourceGrpcClient ?? throw new ArgumentNullException(nameof(resourceGrpcClient));
        _sequenceGroupGrpcClient = sequenceGroupGrpcClient ?? throw new ArgumentNullException(nameof(sequenceGroupGrpcClient));
    }
    
    /// <inheritdoc />
    public async Task ImportAllDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting import of all data from gRPC services");
        
        try
        {
            if (_options.ClearExistingDataBeforeImport)
            {
                await ClearDatabaseAsync(cancellationToken);
            }
            
            // Import all data types
            await ImportSequencesAsync(cancellationToken);
            await ImportParametersAsync(cancellationToken);
            await ImportResourcesAsync(cancellationToken);
            await ImportSequenceGroupsAsync(cancellationToken);
            
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
        _logger.LogInformation("Importing sequences from gRPC service");
        
        try
        {
            // Get sequences from gRPC service
            var sequences = await _sequenceGrpcClient.GetAllSequencesAsync(cancellationToken);
            
            if (sequences == null || !sequences.Any())
            {
                _logger.LogWarning("No sequences returned from gRPC service");
                return;
            }
            
            _logger.LogInformation("Retrieved {Count} sequences from gRPC service", sequences.Count());
            
            // Use domain service to create sequences
            foreach (var sequence in sequences)
            {
                // Check if sequence already exists
                var existingSequence = await _sequenceService.GetSequenceByIdAsync(sequence.Id);
                
                if (existingSequence == null)
                {
                    // Create new sequence
                    await _sequenceService.CreateSequenceAsync(sequence);
                    _logger.LogDebug("Created sequence: {Id} - {Name}", sequence.Id, sequence.Name);
                }
                else
                {
                    // Update existing sequence
                    await _sequenceService.UpdateSequenceAsync(sequence);
                    _logger.LogDebug("Updated sequence: {Id} - {Name}", sequence.Id, sequence.Name);
                }
            }
            
            _logger.LogInformation("Successfully imported {Count} sequences", sequences.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing sequences from gRPC service");
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task ImportParametersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Importing parameters from gRPC service");
        
        try
        {
            // Get parameters from gRPC service
            var parameters = await _parameterGrpcClient.GetAllParametersAsync(cancellationToken);
            
            if (parameters == null || !parameters.Any())
            {
                _logger.LogWarning("No parameters returned from gRPC service");
                return;
            }
            
            _logger.LogInformation("Retrieved {Count} parameters from gRPC service", parameters.Count());
            
            // Use domain service to create parameters
            foreach (var parameter in parameters)
            {
                // Check if parameter already exists
                var existingParameter = await _parameterService.GetParameterByIdAsync(parameter.Id);
                
                if (existingParameter == null)
                {
                    // Create new parameter
                    await _parameterService.CreateParameterAsync(parameter);
                    _logger.LogDebug("Created parameter: {Id} - {Name}", parameter.Id, parameter.Name);
                }
                else
                {
                    // Update existing parameter
                    await _parameterService.UpdateParameterAsync(parameter);
                    _logger.LogDebug("Updated parameter: {Id} - {Name}", parameter.Id, parameter.Name);
                }
            }
            
            _logger.LogInformation("Successfully imported {Count} parameters", parameters.Count());
            
            // Now handle sequence parameters
            var sequences = await _sequenceService.GetAllSequencesAsync();
            foreach (var sequence in sequences)
            {
                var sequenceParameters = await _parameterGrpcClient.GetParametersForSequenceAsync(sequence.Id, cancellationToken);
                
                if (sequenceParameters != null && sequenceParameters.Any())
                {
                    foreach (var seqParam in sequenceParameters)
                    {
                        await _sequenceService.AddParameterToSequenceAsync(
                            seqParam.ParameterId, 
                            seqParam.SequenceId, 
                            seqParam.OrderNumber);
                            
                        _logger.LogDebug("Added parameter {ParameterId} to sequence {SequenceId} with order {OrderNumber}", 
                            seqParam.ParameterId, seqParam.SequenceId, seqParam.OrderNumber);
                    }
                }
            }
            
            _logger.LogInformation("Successfully imported sequence parameters");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing parameters from gRPC service");
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task ImportResourcesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Importing resources from gRPC service");
        
        try
        {
            // Get resources from gRPC service
            var resources = await _resourceGrpcClient.GetAllResourcesAsync(cancellationToken);
            
            if (resources == null || !resources.Any())
            {
                _logger.LogWarning("No resources returned from gRPC service");
                return;
            }
            
            _logger.LogInformation("Retrieved {Count} resources from gRPC service", resources.Count());
            
            // Use domain service to create resources
            foreach (var resource in resources)
            {
                // Check if resource already exists
                var existingResource = await _resourceService.GetResourceByIdAsync(resource.Id);
                
                if (existingResource == null)
                {
                    // Create new resource
                    await _resourceService.CreateResourceAsync(resource);
                    _logger.LogDebug("Created resource: {Id} - {Name}", resource.Id, resource.Name);
                }
                else
                {
                    // Update existing resource
                    await _resourceService.UpdateResourceAsync(resource);
                    _logger.LogDebug("Updated resource: {Id} - {Name}", resource.Id, resource.Name);
                }
            }
            
            _logger.LogInformation("Successfully imported {Count} resources", resources.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing resources from gRPC service");
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task ImportSequenceGroupsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Importing sequence groups from gRPC service");
        
        try
        {
            // Get sequence groups from gRPC service
            var sequenceGroups = await _sequenceGroupGrpcClient.GetAllSequenceGroupsAsync(cancellationToken);
            
            if (sequenceGroups == null || !sequenceGroups.Any())
            {
                _logger.LogWarning("No sequence groups returned from gRPC service");
                return;
            }
            
            _logger.LogInformation("Retrieved {Count} sequence groups from gRPC service", sequenceGroups.Count());
            
            // Use domain service to create sequence groups
            foreach (var group in sequenceGroups)
            {
                // Check if sequence group already exists
                var existingGroup = await _sequenceGroupService.GetSequenceGroupByIdAsync(group.Id);
                
                if (existingGroup == null)
                {
                    // Create new sequence group
                    await _sequenceGroupService.CreateSequenceGroupAsync(group);
                    _logger.LogDebug("Created sequence group: {Id} - {Name}", group.Id, group.Name);
                }
                else
                {
                    // Update existing sequence group
                    await _sequenceGroupService.UpdateSequenceGroupAsync(group);
                    _logger.LogDebug("Updated sequence group: {Id} - {Name}", group.Id, group.Name);
                }
            }
            
            _logger.LogInformation("Successfully imported {Count} sequence groups", sequenceGroups.Count());
            
            // Now handle sequence group sequences
            var sequenceGroupSequences = await _sequenceGroupGrpcClient.GetSequenceGroupSequencesAsync(cancellationToken);
            
            if (sequenceGroupSequences != null && sequenceGroupSequences.Any())
            {
                foreach (var groupSeq in sequenceGroupSequences)
                {
                    await _sequenceGroupService.AddSequenceToGroupAsync(
                        groupSeq.SequenceId, 
                        groupSeq.SequenceGroupId, 
                        groupSeq.OrderNumber);
                        
                    _logger.LogDebug("Added sequence {SequenceId} to group {GroupId} with order {OrderNumber}", 
                        groupSeq.SequenceId, groupSeq.SequenceGroupId, groupSeq.OrderNumber);
                }
                
                _logger.LogInformation("Successfully imported {Count} sequence group mappings", 
                    sequenceGroupSequences.Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing sequence groups from gRPC service");
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task<Dictionary<string, bool>> TestConnectionsAsync(CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, bool>();
        
        try
        {
            results["SequenceService"] = await _sequenceGrpcClient.TestConnectionAsync(cancellationToken);
            results["ParameterService"] = await _parameterGrpcClient.TestConnectionAsync(cancellationToken);
            results["ResourceService"] = await _resourceGrpcClient.TestConnectionAsync(cancellationToken);
            results["SequenceGroupService"] = await _sequenceGroupGrpcClient.TestConnectionAsync(cancellationToken);
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing gRPC service connections");
            throw;
        }
    }
    
    /// <summary>
    /// Clears the database before import
    /// </summary>
    private async Task ClearDatabaseAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Clearing database before import");
        
        try
        {
            // Clear data in reverse order of dependencies to respect foreign key constraints
            _dbContext.SequenceGroupCollectionSequenceGroups.RemoveRange(_dbContext.SequenceGroupCollectionSequenceGroups);
            _dbContext.SequenceGroupSequences.RemoveRange(_dbContext.SequenceGroupSequences);
            _dbContext.SequenceParameters.RemoveRange(_dbContext.SequenceParameters);
            _dbContext.RangeValues.RemoveRange(_dbContext.RangeValues);
            _dbContext.Parameters.RemoveRange(_dbContext.Parameters);
            _dbContext.Ranges.RemoveRange(_dbContext.Ranges);
            _dbContext.Resources.RemoveRange(_dbContext.Resources);
            _dbContext.Sequences.RemoveRange(_dbContext.Sequences);
            _dbContext.SequenceGroupCollections.RemoveRange(_dbContext.SequenceGroupCollections);
            _dbContext.SequenceGroups.RemoveRange(_dbContext.SequenceGroups);

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Database cleared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing database before import");
            throw;
        }
    }
}