namespace Instrument.Data.Orchestration.ConfigurationImport.Steps;
using System;
using System.Threading;
using System.Threading.Tasks;
using Instrument.Execution.Grpc.Configuration;
using Instrument.Data.Grpc;
using Instrument.Data.Orchestration;
using Instrument.Data.Orchestration.ConfigurationImport;
using Microsoft.Extensions.Logging;


/// <summary>
/// Step to fetch configuration from ExecutionConfigurationService
/// </summary>
public class GetConfigurationStep : IOrchestrationStep
{
    private readonly IGrpcGateway _gateway;
    private readonly ILogger<GetConfigurationStep> _logger;

    public GetConfigurationStep(IGrpcGateway gateway,
        ILogger<GetConfigurationStep> logger)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string StepName => "GetConfiguration";

    public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
    {
        var request = context.GetData<ConfigurationImportRequest>("ImportRequest");

        try
        {
            var operation = _gateway.ExecutionConfigurationOperations.CreateGetCurrentConfigurationOperation();
            var grpcRequest = new FetchExecutionConfigurationRequest(request is { IncludeSequences: true });

            var result = await _gateway.ExecuteAsync(operation, grpcRequest, cancellationToken);

            if (!result.IsSuccess)
            {
                return StepResult.Failure($"Failed to fetch configuration: {result.ErrorMessage}");
            }

            if (result.Data?.Configuration == null)
            {
                return StepResult.Failure("Configuration is null in response");
            }

            // Store the fetched configuration and metadata in context
            context.SetData("FetchedConfiguration", result.Data.Configuration);
            context.SetData("RequestId", $"{result.Data.RequestId}");

            _logger.LogInformation("Configuration fetched successfully. Sequences: {SequenceCount}",
                result.Data.Configuration.Sequences.Count);

            return StepResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch configuration from ExecutionConfigurationService");
            return StepResult.Failure($"Failed to fetch configuration: {ex.Message}");
        }
    }
}
