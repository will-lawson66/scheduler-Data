namespace Instrument.Scheduling.Data.Entities;
public class SequenceGroup : SequenceGroupBase
{
    /// <summary>
    /// An ordered list of sequences to be performed as a group.
    /// </summary>
    public required SortedList<int, Sequence> Sequences { get; set; }

    /// <summary>
    /// An optional integer representing the period in which to execute the sequences.
    /// May be relative or absolute - the scheduler will compute this property at runtime.
    /// </summary>
    public int? Period { get; set; }
}
