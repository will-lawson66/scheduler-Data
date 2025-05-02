namespace Instrument.Scheduling.Data.Entities;
public abstract record SequenceGroupBase
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}
