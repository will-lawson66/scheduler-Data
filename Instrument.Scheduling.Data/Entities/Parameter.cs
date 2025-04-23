namespace Instrument.Scheduling.Data.Entities;

public record Parameter
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string ParameterType { get; init; } // string, number, boolean, etc.
    public string? DefaultValue { get; init; }
    public string? MinValue { get; init; }
    public string? MaxValue { get; init; }
    public bool Required { get; init; }
    public string? Description { get; init; }
    
    // Navigation property for the many-to-many relationship
    public List<SequenceParameter> SequenceParameters { get; init; } = new();
}
