namespace Instrument.Data.Orchestration.ConfigurationImport.Steps;
using System;
using System.Threading;
using System.Threading.Tasks;
using Instrument.Scheduling.Data;
using Instrument.Data.Orchestration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Step to initialize database if needed
/// </summary>
public class InitializeDatabaseStep : IOrchestrationStep
{
    private readonly IDataInitializer _dataInitializer;
    private readonly ILogger<InitializeDatabaseStep> _logger;

    public InitializeDatabaseStep(
        IDataInitializer dataInitializer,
        ILogger<InitializeDatabaseStep> logger)
    {
        _dataInitializer = dataInitializer;
        _logger = logger;
    }

    public string StepName => "InitializeDatabase";

    public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
    {
        try
        {
            await _dataInitializer.InitializeAsync();
            _logger.LogDebug("Database initialization completed");
            return StepResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            return StepResult.Failure($"Failed to initialize database: {ex.Message}");
        }
    }
}
