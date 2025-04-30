namespace Instrument.Scheduling.Data.Entities;

public record RangeValue
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public required string Value { get; set; }
    public required string RangeId { get; init; }
    
    // Navigation property
    public Range Range { get; init; } = null!;
}
