using Instrument.Data.Entities;

namespace Instrument.Data;

public interface ISequenceService
{
    Task<Sequence?> GetSequenceByIdAsync(string id);
    Task CreateSequenceAsync(Sequence sequence);
    Task UpdateSequenceAsync(Sequence sequence);
    Task DeleteSequenceAsync(string id);
    Task<IEnumerable<Sequence>> GetAllSequencesAsync();
}
