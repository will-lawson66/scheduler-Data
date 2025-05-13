namespace Instrument.Data.Entities;

/// <summary>
/// Base class for sequence group collections
/// </summary>
public abstract record SequenceGroupCollectionBase : SequenceAggregateBase
{
    // The full type name of TEnum collection category
    public string? CategoryTypeName { get; init; }

    // String representation of the TEnum value
    public string? CategoryName { get; init; }

    // Navigation property 
    public List<SequenceGroupCollectionSequenceGroup> SequenceGroupCollectionSequenceGroups { get; set; } = [];
}
