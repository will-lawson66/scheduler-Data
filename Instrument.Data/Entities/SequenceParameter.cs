using System.ComponentModel.DataAnnotations;

namespace Instrument.Data.Entities;

public record SequenceParameter
{
    // Composite key will be configured in DbContext
    public required int SequenceId { get; init; }
    public required int ParameterId { get; init; }
    public int OrderNumber { get; set; }

    // Navigation properties
    public Sequence? Sequence { get; set; }
    public Parameter? Parameter { get; set; } = null!;
}
