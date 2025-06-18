namespace Instrument.Data.Orchestration.ConfigurationImport.Steps;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Step to validate the import request
/// </summary>
public class ValidateRequestStep : IOrchestrationStep
{
    private readonly ILogger<ValidateRequestStep> _logger;

    public ValidateRequestStep(ILogger<ValidateRequestStep> logger)
    {
        _logger = logger;
    }

    public string StepName => "ValidateRequest";

    public Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
    {
        var request = context.GetData<ConfigurationImportRequest>("ImportRequest");

        if (request == null)
        {
            return Task.FromResult(StepResult.Failure("Import request is null"));
        }

        _logger.LogDebug("Request validation completed successfully. IncludeSequences: {IncludeSequences}, ClearData: {ClearData}",
            request.IncludeSequences, request.ClearExistingData);

        return Task.FromResult(StepResult.Success());
    }
}
