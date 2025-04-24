namespace Instrument.Scheduling.Data.Entities;

public record SequenceParameter
{
    // Composite key will be configured in DbContext
    public required string SequenceId { get; init; }
    public required string ParameterId { get; init; }
    public int OrderNumber { get; init; }
    
    // Navigation properties
    public Sequence Sequence { get; init; } = null!;
    public Parameter Parameter { get; init; } = null!;
}
