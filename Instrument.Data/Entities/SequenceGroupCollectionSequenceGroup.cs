using System.ComponentModel.DataAnnotations;

namespace Instrument.Data.Entities;

public class SequenceGroupCollectionSequenceGroup
{
    public required int SequenceGroupCollectionId { get; init; }
    public required int SequenceGroupId { get; init; }
    public required int Order { get; set; }

    // Navigation properties
    public SequenceGroupCollectionBase? SequenceGroupCollection { get; set; }
    public SequenceGroup? SequenceGroup { get; set; }
}
