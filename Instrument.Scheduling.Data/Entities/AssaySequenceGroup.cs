namespace Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Entities.Enums;

public class AssaySequenceGroup : SequenceGroupBase
{
    /// <summary>
    /// The assay technology (ImmunoCap, Elia) for this assay
    /// </summary>
    public required Technology AssayTechnology { get; init; }

    public required Dictionary<ErwStation, SequenceGroup> ErwSequenceGroups { get; init; }

    public required Dictionary<IrwStation, SequenceGroup> IrwSequenceGroups { get; init; }
}
