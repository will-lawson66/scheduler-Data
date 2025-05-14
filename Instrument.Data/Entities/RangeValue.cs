using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Instrument.Data.Entities;

public record RangeValue
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Value { get; init; }
    public required int RangeId { get; init; }
    
    // Navigation property
    public Range Range { get; set; } = null!;
    
    // Update method - returns a new instance with the specified changes
    public RangeValue Update(string? name = null, string? value = null)
    {
        // Validate updates if needed
        if (name != null && string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty", nameof(name));
        }

        if (value != null && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty", nameof(value));
        }
            
        // Use record's "with" expression for creating a modified copy
        return this with 
        {
            Name = name ?? Name,
            Value = value ?? Value
        };
    }
}
