using System.ComponentModel.DataAnnotations;

namespace Instrument.Data.Entities;

public record SequenceParameter
{
    // Composite key properties with explicit order and annotations
    [Key]
    [Column(Order = 0)]
    [ForeignKey(nameof(Sequence))]
    public required int SequenceId { get; init; }
    
    [Key]
    [Column(Order = 1)]
    [ForeignKey(nameof(Parameter))]
    public required int ParameterId { get; init; }
    public int OrderNumber { get; set; }

    // Navigation properties
    public Sequence? Sequence { get; set; }
    public Parameter? Parameter { get; set; } = null!;
}
