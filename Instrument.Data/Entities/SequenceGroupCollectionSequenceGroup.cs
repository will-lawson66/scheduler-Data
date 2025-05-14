using System.ComponentModel.DataAnnotations;

namespace Instrument.Data.Entities;

public class SequenceGroupCollectionSequenceGroup
{
    [Key]
    [Column(Order = 0)]
    [ForeignKey(nameof(SequenceGroupCollection))]
    public required int SequenceGroupCollectionId { get; init; }
    
    [Key]
    [Column(Order = 1)]
    [ForeignKey(nameof(SequenceGroup))]
    public required int SequenceGroupId { get; init; }
    public required int Order { get; set; }

    // Navigation properties
    public SequenceGroupCollectionBase? SequenceGroupCollection { get; set; }
    public SequenceGroup? SequenceGroup { get; set; }
}
