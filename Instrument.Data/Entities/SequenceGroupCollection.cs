namespace Instrument.Data.Entities;

using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Generic sequence group collection with enum-based category
/// </summary>
/// <typeparam name="TEnum">The enum type used for categorization</typeparam>
public record SequenceGroupCollection<TEnum> : SequenceGroupCollectionBase where TEnum : Enum
{
    public TEnum Category
    {
        get => (TEnum)Enum.Parse(typeof(TEnum), CategoryName ?? string.Empty);
        init
        {
            CategoryName = value.ToString();
            CategoryTypeName = typeof(TEnum).FullName ?? typeof(TEnum).Name;
        }
    }
}

