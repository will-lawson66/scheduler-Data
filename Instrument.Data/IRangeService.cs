namespace Instrument.Data;

public interface IRangeService
{
    Task<Entities.Range?> GetRangeByIdAsync(int id);

    Task<Entities.Range?> GetRangeWithRangeValuesAsync(int id);

    Task CreateRangeAsync(Entities.Range range);

    Task UpdateRangeAsync(Entities.Range range);

    Task DeleteRangeAsync(int id);

    Task<IEnumerable<Entities.Range>> GetAllRangesAsync();
}
