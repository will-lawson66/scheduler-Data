using System.ComponentModel.DataAnnotations;

namespace Instrument.Data.Entities;

public class SequenceGroupSequence
{
    [Key]
    [Column(Order = 0)]
    [ForeignKey(nameof(SequenceGroup))]
    public required int SequenceGroupId { get; init; }
    
    [Key]
    [Column(Order = 1)]
    [ForeignKey(nameof(Sequence))]
    public required int SequenceId { get; init; }
    public required int Order { get; set; }
    
    // Navigation properties
    public SequenceGroup? SequenceGroup { get; set; }
    public Sequence? Sequence { get; set; }
}
