namespace Instrument.Data.Entities;

public class SequenceGroupSequence
{
    public required string SequenceGroupId { get; init; }
    public required string SequenceId { get; init; }
    public required int Order { get; init; }
    
    // Navigation properties
    public SequenceGroup? SequenceGroup { get; set; }
    public Sequence? Sequence { get; set; }
}
