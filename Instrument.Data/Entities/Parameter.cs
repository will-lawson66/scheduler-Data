using Instrument.Data.Entities.Enums;

namespace Instrument.Data.Entities;


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
    public string? ResourceId { get; set; }
    
    // Navigation properties
    public Range? Range { get; init; }
    public Resource? Resource { get; init; }
    public List<SequenceParameter> SequenceParameters { get; set; } = [];
    
    // Update method - returns a new instance with the specified changes
    public Parameter Update(
        string? name = null, 
        ParameterType? type = null, 
        string? min = null, 
        string? max = null, 
        string? defaultValue = null, 
        string? format = null, 
        string? rangeId = null,
        string? resourceId = null)
    {
        // Validate updates if needed
        if (name != null && string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty", nameof(name));
        }
            
        // Use record's "with" expression for creating a modified copy
        return this with 
        {
            Name = name ?? Name,
            Type = type ?? Type,
            Min = min ?? Min,
            Max = max ?? Max,
            DefaultValue = defaultValue ?? DefaultValue,
            Format = format ?? Format,
            RangeId = rangeId ?? RangeId,
            ResourceId = resourceId ?? ResourceId
        };
    }
}
