using Instrument.Data.Entities;

namespace Instrument.Data;

public interface ISequenceService
{
    Task<Sequence?> GetSequenceByIdAsync(int id);
    Task CreateSequenceAsync(Sequence sequence);
    Task UpdateSequenceAsync(Sequence sequence);
    Task DeleteSequenceAsync(int id);
    Task<IEnumerable<Sequence>> GetAllSequencesAsync();
}
