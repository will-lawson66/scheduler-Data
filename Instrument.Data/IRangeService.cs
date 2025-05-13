namespace Instrument.Data;

public interface IRangeService
{
    Task<Entities.Range?> GetRangeByIdAsync(string id);

    Task<Entities.Range?> GetRangeWithRangeValuesAsync(string id);

    Task CreateRangeAsync(Entities.Range range);

    Task UpdateRangeAsync(Entities.Range range);

    Task DeleteRangeAsync(string id);

    Task<IEnumerable<Entities.Range>> GetAllRangesAsync();
}
