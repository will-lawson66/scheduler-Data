using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instrument\.Data.Entities;

public class SequenceGroupSequences
{
    public required string SequenceGroupId { get; init; }
    public required string SequenceId { get; init; }
    public required int Order { get; init; }
    
    // Navigation properties
    public SequenceGroup? SequenceGroup { get; init; }
    public Sequence? Sequence { get; init; }
}
