namespace Instrument.Data.Entities;

public class SequenceGroupCollectionSequenceGroup
{
    public required string SequenceGroupCollectionId { get; init; }
    public required string SequenceGroupId { get; init; }
    public required int Order { get; init; }

    // Navigation properties
    public SequenceGroupCollectionBase? SequenceGroupCollection { get; set; }
    public SequenceGroup? SequenceGroup { get; set; }
}
