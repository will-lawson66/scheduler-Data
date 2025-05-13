namespace Instrument.Data.Entities;

public record SequenceParameter
{
    // Composite key will be configured in DbContext
    public required string SequenceId { get; init; }
    public required string ParameterId { get; init; }
    public int OrderNumber { get; set; }

    // Navigation properties
    public Sequence? Sequence { get; set; }
    public Parameter? Parameter { get; set; } = null!;
}
