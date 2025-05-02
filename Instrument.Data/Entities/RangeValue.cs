namespace Instrument\.Data.Entities;

public record RangeValue
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public required string Value { get; set; }
    public required string RangeId { get; init; }
    
    // Navigation property
    public Range Range { get; init; } = null!;
    
    // Update method - returns a new instance with the specified changes
    public RangeValue Update(string? name = null, string? value = null)
    {
        // Validate updates if needed
        if (name != null && string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
            
        if (value != null && string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be empty", nameof(value));
            
        // Use record's "with" expression for creating a modified copy
        return this with 
        {
            Name = name ?? Name,
            Value = value ?? Value
        };
    }
}
