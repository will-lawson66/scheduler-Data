using Instrument.Scheduling.Data.Entities.Enums;

namespace Instrument.Scheduling.Data.Entities;


public record Parameter
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required ParameterType Type { get; init; }
    public string? Min { get; init; }
    public string? Max { get; init; }
    public string? DefaultValue { get; init; }
    public string? Format { get; init; }
    public string? RangeId { get; init; }
    public string? ResourceId { get; init; }
    
    // Navigation properties
    public Range? Range { get; init; }
    public Resource? Resource { get; init; }
    public List<SequenceParameter> SequenceParameters { get; init; } = [];
}
