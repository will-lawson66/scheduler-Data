namespace Instrument.Scheduling.Data.Entities;
public class SequenceGroup : SequenceGroupBase
{
    // Navigation property for the one-to-many relationship with SequenceGroupSequences
    public List<SequenceGroupSequences> SequenceGroupSequences { get; init; } = [];
}
