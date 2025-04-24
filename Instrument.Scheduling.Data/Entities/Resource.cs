namespace Instrument.Scheduling.Data.Entities;

public record Resource
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public bool Locked { get; init; }
    
    // Navigation property
    public List<Parameter> Parameters { get; init; } = new();
}
