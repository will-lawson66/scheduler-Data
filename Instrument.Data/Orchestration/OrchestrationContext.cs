namespace Instrument.Data.Orchestration;
using System.Collections.Generic;

/// <summary>
/// Context passed between orchestration steps
/// </summary>
public class OrchestrationContext
{
    public Dictionary<string, object?> Data { get; } = [];
    public List<string> CompletedSteps { get; } = [];
    public List<string> Errors { get; } = [];

    public T? GetData<T>(string key)
    {
        return Data.TryGetValue(key, out var value) ? (T?)value : default;
    }

    public void SetData<T>(string key, T value)
    {
        Data[key] = value;
    }
}
