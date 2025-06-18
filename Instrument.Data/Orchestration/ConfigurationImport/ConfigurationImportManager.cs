namespace Instrument.Data.Orchestration.ConfigurationImport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Instrument.Data.Orchestration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implements the Process Manager pattern for orchestrating configuration import
/// </summary>
public class ConfigurationImportManager : IProcessManager<ConfigurationImportRequest, ConfigurationImportResult>
{
    private readonly IEnumerable<IOrchestrationStep> _steps;
    private readonly ILogger<ConfigurationImportManager> _logger;

    public ConfigurationImportManager(
        IEnumerable<IOrchestrationStep> steps,
        ILogger<ConfigurationImportManager> logger)
    {
        _steps = steps.OrderBy(s => GetStepOrder(s.StepName));
        _logger = logger;
    }

    public async Task<ConfigurationImportResult> ExecuteAsync(
        ConfigurationImportRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var context = new OrchestrationContext();
        var result = new ConfigurationImportResult();

        // Store request in context
        context.SetData("ImportRequest", request);
        context.SetData("Statistics", new ImportStatistics());

        _logger.LogInformation("Starting configuration import process with {StepCount} steps", _steps.Count());

        try
        {
            foreach (var step in _steps)
            {
                _logger.LogDebug("Executing step: {StepName}", step.StepName);

                var stepResult = await step.ExecuteAsync(context, cancellationToken);
                context.CompletedSteps.Add(step.StepName);

                if (!stepResult.IsSuccess)
                {
                    var error = $"Step '{step.StepName}' failed: {stepResult.ErrorMessage}";
                    context.Errors.Add(error);
                    _logger.LogError(error);

                    if (!stepResult.ShouldContinue)
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = error;
                        break;
                    }
                }
                else
                {
                    _logger.LogDebug("Step {StepName} completed successfully", step.StepName);
                }
            }

            // If all steps completed check for any errors
            if (result.ErrorMessage == null)
            {
                result.IsSuccess = context.Errors.Count == 0;
                if (!result.IsSuccess)
                {
                    result.ErrorMessage = string.Join("; ", context.Errors);
                }
            }

            // Extract results from context
            result.Statistics = context.GetData<ImportStatistics>("Statistics") ?? new ImportStatistics();
            result.ProcessedSteps = [..context.CompletedSteps];
            result.RequestId = context.GetData<string>("RequestId") ?? Guid.NewGuid().ToString();

            _logger.LogInformation("Configuration import process completed. Success: {Success}, Duration: {Duration}ms",
                result.IsSuccess, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration import process failed with unexpected error");
            result.IsSuccess = false;
            result.ErrorMessage = $"Unexpected error: {ex.Message}";
        }
        finally
        {
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }

    private static int GetStepOrder(string stepName) => stepName switch
    {
        "ValidateRequest" => 1,
        "ClearExistingData" => 2,
        "InitializeDatabase" => 3,
        "GetConfiguration" => 4,
        "ImportSequences" => 5,
        "ImportResources" => 6,
        _ => 999
    };
}
