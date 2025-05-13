using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Data.Repository;

/// <summary>
/// Repository for range values
/// </summary>
public class RangeValueRepository : Repository<RangeValue>, IRangeValueRepository
{
    /// <inheritdoc />
    public RangeValueRepository(SchedulerDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RangeValue>> GetRangeValuesByRangeIdAsync(string rangeId)
    {
        return await DbContext.RangeValues
            .Where(rv => rv.RangeId == rangeId)
            .ToListAsync();
    }
}