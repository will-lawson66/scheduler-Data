namespace Instrument.Data.Orchestration;

/// <summary>
/// Result of a single orchestration step
/// </summary>
public class StepResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public bool ShouldContinue { get; set; } = true;

    public static StepResult Success()
    {
        return new StepResult
        {
            IsSuccess = true
        };
    }

    public static StepResult Failure(string error, bool shouldContinue = false)
    {
        return new StepResult
        {
            IsSuccess = false,
            ErrorMessage = error,
            ShouldContinue = shouldContinue
        };
    }
}
