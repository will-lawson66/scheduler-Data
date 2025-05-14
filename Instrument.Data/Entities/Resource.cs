using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Instrument.Data.Entities;

public record Resource
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public bool Locked { get; init; }
    
    // Navigation property
    public List<Parameter> Parameters { get; set; } = [];
    
    // Update method - returns a new instance with the specified changes
    public Resource Update(string? name = null, string? code = null, bool? locked = null)
    {
        // Validate updates if needed
        if (name != null && string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty", nameof(name));
        }

        if (code != null && string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code cannot be empty", nameof(code));
        }
            
        // Use record's "with" expression for creating a modified copy
        return this with 
        {
            Name = name ?? Name,
            Code = code ?? Code,
            Locked = locked ?? Locked
        };
    }
}
