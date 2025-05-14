using System.ComponentModel.DataAnnotations;

namespace Instrument.Data.Entities;

public class SequenceGroupSequence
{
    public required int SequenceGroupId { get; init; }
    public required int SequenceId { get; init; }
    public required int Order { get; set; }
    
    // Navigation properties
    public SequenceGroup? SequenceGroup { get; set; }
    public Sequence? Sequence { get; set; }
}
