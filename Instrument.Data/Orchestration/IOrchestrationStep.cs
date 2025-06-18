namespace Instrument.Data.Orchestration;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents a step in the orchestration process
/// </summary>
public interface IOrchestrationStep
{
    /// <summary>
    /// Name of this step.
    /// </summary>
    string StepName { get; }

    /// <summary>
    /// Execute the step
    /// </summary>
    /// <param name="context">An <see cref="OrchestrationContext"/></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken);
}
