namespace Instrument.Scheduling.Data.Entities;
public record Sequence
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public TimeSpan WorstCaseTime { get; init; } = TimeSpan.FromMilliseconds(30000);
}

public record SequenceParameter
{
    public string Id { get; set; } = default!;
    public string Type { get; set; } = default!;
    public int Order { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}
