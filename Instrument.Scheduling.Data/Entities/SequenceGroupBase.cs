namespace Instrument.Scheduling.Data.Entities;
public abstract class SequenceGroupBase
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}
