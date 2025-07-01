using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;

namespace Instrument.Data.DTOs;

public class SequenceGroupDTO
{
    public required string Name { get; set; }
    public Technology? Technology { get; set; }
    public IEnumerable<Sequence> Sequences { get; set; } = [];
}