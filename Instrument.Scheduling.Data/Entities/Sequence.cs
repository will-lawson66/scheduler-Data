namespace Instrument.Scheduling.Data.Entities;
public record Sequence
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public TimeSpan WorstCaseTime { get; init; } = TimeSpan.FromMilliseconds(30000);
    public bool CanBeParallel { get; init; } = false;
    
    // Navigation property for the many-to-many relationship with Parameters
    public List<SequenceParameter> SequenceParameters { get; init; } = new();
}