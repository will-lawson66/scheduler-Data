using Instrument.Data.DataContext;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Data.Repository;

/// <summary>
/// Repository for ranges
/// </summary>
public class RangeRepository : Repository<Entities.Range>, IRangeRepository
{
    /// <summary>
    /// Creates a new range repository
    /// </summary>
    /// <param name="dbContext">Database context</param>
    public RangeRepository(SchedulerDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <inheritdoc />
    public async Task<Entities.Range?> GetRangeWithRangeValuesByIdAsync(string rangeId)
    {
        return await DbContext.Ranges
            .Include(r => r.RangeValues)
            .FirstOrDefaultAsync(r => r.Id == rangeId);
    }
}