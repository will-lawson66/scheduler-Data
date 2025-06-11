namespace Instrument.Data.Orchestration.ConfigurationImport.Steps;
using System;
using System.Threading;
using System.Threading.Tasks;
using Instrument.Data.Orchestration;
using Instrument.Data.Orchestration.ConfigurationImport;
using Instrument.Scheduling.Data.Services.Cleanup;
using Microsoft.Extensions.Logging;

/// <summary>
/// Step to clear existing data if requested
/// </summary>
public class TruncateDataStep : IOrchestrationStep
{
    private readonly ILogger<TruncateDataStep> _logger;
    private readonly DatabaseCleanupService _dbCleanupService;

    public TruncateDataStep(
        ILogger<TruncateDataStep> logger,
        DatabaseCleanupService dbCleanupService)
    {
        _logger = logger;
        _dbCleanupService = dbCleanupService;
    }

    public string StepName => "ClearExistingData";

    public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
    {
        var request = context.GetData<ConfigurationImportRequest>("ImportRequest");

        if (request is { ClearExistingData: false })
        {
            _logger.LogDebug("Skipping data clearing as has not been requested");
            return StepResult.Success();
        }

        try
        {
            await _dbCleanupService.ClearAllDataAsync();
            _logger.LogInformation("Existing data cleared successfully");
            return StepResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear existing data");
            return StepResult.Failure($"Failed to clear existing data: {ex.Message}", shouldContinue: true);
        }
    }
}
