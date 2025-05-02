namespace Instrument.Data.Entities;

public record Range
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    
    // Navigation properties
    public List<RangeValue> Values { get; init; } = [];
    public List<Parameter> Parameters { get; init; } = [];
    
    // Update method - returns a new instance with the specified changes
    public Range Update(string? name = null, string? description = null)
    {
        // Validate updates if needed
        if (name != null && string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
            
        // Use record's "with" expression for creating a modified copy
        return this with 
        {
            Name = name ?? Name,
            Description = description ?? Description
        };
    }
}
