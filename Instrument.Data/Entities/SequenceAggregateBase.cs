namespace Instrument.Data.Entities;
public abstract record SequenceAggregateBase
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}
