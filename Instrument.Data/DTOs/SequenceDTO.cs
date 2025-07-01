using Instrument.Data.Entities;

namespace Instrument.Data.DTOs;

public class SequenceDTO
{
    public required string Name { get; set; }
    public int? Order { get; set; }
    public IEnumerable<Parameter> Parameters { get; set; } = [];
}