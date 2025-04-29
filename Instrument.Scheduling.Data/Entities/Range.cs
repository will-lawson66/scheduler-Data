namespace Instrument.Scheduling.Data.Entities;

public record Range
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    
    // Navigation properties
    public List<RangeValue> Values { get; init; } = [];
    public List<Parameter> Parameters { get; init; } = [];
}
