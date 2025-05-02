namespace Instrument.Data.Entities;
public record SequenceGroup : SequenceGroupBase
{
    // Navigation property for the one-to-many relationship with SequenceGroupSequences
    public List<SequenceGroupSequences> SequenceGroupSequences { get; init; } = [];
    
    // Update method - returns a new instance with the specified changes
    public SequenceGroup Update(string? name = null, string? description = null)
    {
        // Validate updates if needed
        if (name != null && string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
            
        // Create a new instance with updated properties
        return this with
        {
            Name = name ?? Name,
            Description = description ?? Description
        };
    }
}
