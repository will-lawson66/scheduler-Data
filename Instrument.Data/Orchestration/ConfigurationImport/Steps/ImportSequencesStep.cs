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
/// Step to import sequences from ExecutionService to scheduler-Data entities
/// </summary>
public class ImportSequencesStep : IOrchestrationStep
{
    private readonly ISequenceService _sequenceService;
    private readonly ILogger<ImportSequencesStep> _logger;

    public ImportSequencesStep(
        ISequenceService sequenceService,
        ILogger<ImportSequencesStep> logger)
    {
        _sequenceService = sequenceService;
        _logger = logger;
    }

    public string StepName => "ImportSequences";

    public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
    {
        try
        {
            var configuration = context.GetData<ExecutionConfigurationContract>("FetchedConfiguration");
            var statistics = context.GetData<ImportStatistics>("Statistics");
            var request = context.GetData<ConfigurationImportRequest>("ImportRequest");

            if (configuration?.Sequences == null)
            {
                return StepResult.Failure("No configuration or sequences found in context");
            }

            var sequencesToProcess = configuration.Sequences;

            // Apply filters if specified
            if (request?.SequenceFilters?.Count != 0)
            {
                sequencesToProcess = sequencesToProcess
                    .Where(s => request is { SequenceFilters: not null } && request.SequenceFilters.Contains(s.Key))
                    .ToList();
            }

            foreach (var sequenceContract in sequencesToProcess)
            {
                await ProcessSequenceAsync(sequenceContract);
                if (statistics != null)
                {
                    statistics.SequencesProcessed++;
                }
            }

            context.SetData("Statistics", statistics);

            _logger.LogInformation("Imported {Count} sequences successfully", statistics?.SequencesProcessed);
            return StepResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import sequences");
            return StepResult.Failure($"Failed to import sequences: {ex.Message}");
        }
    }

    private async Task ProcessSequenceAsync(ExecutionSequenceContract sequenceContract)
    {
        // Map ExecutionSequenceContract to scheduler-Data Sequence entity
        var sequence = new Sequence
        {
            Name = sequenceContract.Key,
            WorstCaseTime = sequenceContract.WorstCaseTime,
            Description = $"Imported from ExecutionService - Method: {sequenceContract.ExecutionMethod}",
            CanBeParallel = false // Default value - could be enhanced based on ExecutionMethod
        };

        // Check if sequence already exists by name
        var existingSequences = await _sequenceService.GetAllSequencesAsync();
        var existingSequence = existingSequences.FirstOrDefault(s => s.Name == sequence.Name);

        if (existingSequence == null)
        {
            await _sequenceService.CreateSequenceAsync(sequence);
            _logger.LogDebug("Created new sequence: {SequenceName}", sequence.Name);
        }
        else
        {
            // Update existing sequence
            var updatedSequence = existingSequence.Update(
                name: sequence.Name,
                worstCaseTime: sequence.WorstCaseTime,
                description: sequence.Description,
                canBeParallel: sequence.CanBeParallel);

            await _sequenceService.UpdateSequenceAsync(updatedSequence);
            _logger.LogDebug("Updated existing sequence: {SequenceName}", sequence.Name);
        }
    }
}
