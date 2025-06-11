namespace Instrument.Data.Orchestration.ConfigurationImport.Steps;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Instrument.Execution.Grpc;
using Instrument.Execution.Grpc.Configuration;
using Instrument.Scheduling.Data;
using Instrument.Scheduling.Data.Entities;
using Instrument.Data.Orchestration;
using Instrument.Data.Orchestration.ConfigurationImport;
using Microsoft.Extensions.Logging;

/// <summary>
/// Step to import resources from ExecutionService to scheduler-Data entities
/// </summary>
public class ImportResourcesStep : IOrchestrationStep
{
    private readonly IResourceService _resourceService;
    private readonly ILogger<ImportResourcesStep> _logger;

    public ImportResourcesStep(
        IResourceService resourceService,
        ILogger<ImportResourcesStep> logger)
    {
        _resourceService = resourceService;
        _logger = logger;
    }

    public string StepName => "ImportResources";

    public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
    {
        try
        {
            var configuration = context.GetData<ExecutionConfigurationContract>("FetchedConfiguration");
            var statistics = context.GetData<ImportStatistics>("Statistics");
            var request = context.GetData<ConfigurationImportRequest>("ImportRequest");

            if (configuration?.Sequences == null)
            {
                return StepResult.Failure("No configuration found in context");
            }

            // Extract unique resources from all sequences
            var allResources = configuration.Sequences
                .SelectMany(s => s.Resources)
                .GroupBy(r => r.Key)
                .Select(g => g.First())
                .ToList();

            // Apply filters if specified
            if (request?.ResourceFilters?.Count != 0)
            {
                allResources = allResources
                    .Where(r => request is { ResourceFilters: not null } && request.ResourceFilters.Contains(r.Key))
                    .ToList();
            }

            foreach (var resourceContract in allResources)
            {
                await ProcessResourceAsync(resourceContract);
                if (statistics != null)
                {
                    statistics.ResourcesProcessed++;
                }
            }

            context.SetData("Statistics", statistics);

            _logger.LogInformation("Imported {Count} resources successfully", statistics?.ResourcesProcessed);
            return StepResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import resources");
            return StepResult.Failure($"Failed to import resources: {ex.Message}");
        }
    }

    private async Task ProcessResourceAsync(ExecutionResourceContract resourceContract)
    {
        // Map ExecutionResourceContract to scheduler-Data Resource entity
        var resource = new Resource
        {
            Name = resourceContract.Key,
            Code = resourceContract.Key, // Using Key as Code
            Locked = !resourceContract.HasScriptingInterface // If no scripting interface, consider it locked
        };

        // Check if resource already exists by code
        var existingResource = await _resourceService.GetByCodeAsync(resource.Code);

        if (existingResource == null)
        {
            await _resourceService.CreateResourceAsync(resource);
            _logger.LogDebug("Created new resource: {ResourceName}", resource.Name);
        }
        else
        {
            // Update existing resource
            var updatedResource = existingResource.Update(
                name: resource.Name,
                code: resource.Code,
                locked: resource.Locked);

            await _resourceService.UpdateResourceAsync(updatedResource);
            _logger.LogDebug("Updated existing resource: {ResourceName}", resource.Name);
        }
    }
}
