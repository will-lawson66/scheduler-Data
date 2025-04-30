using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Scheduling.Data.Repository;

/// <summary>
/// Repository for range values
/// </summary>
public class RangeValueRepository : Repository<RangeValue>, IRangeValueRepository
{
    /// <summary>
    /// Creates a new range value repository
    /// </summary>
    /// <param name="dbContext">Database context</param>
    public RangeValueRepository(SchedulerDbContext dbContext)
        : base(dbContext)
    {
    }
    
    /// <summary>
    /// Gets range values by range ID
    /// </summary>
    /// <param name="rangeId">Range ID</param>
    public async Task<IEnumerable<RangeValue>> GetValuesByRangeIdAsync(string rangeId)
    {
        return await DbContext.RangeValues
            .Where(rv => rv.RangeId == rangeId)
            .ToListAsync();
    }
    
    /// <summary>
    /// Gets values for a range (alternative naming for GetValuesByRangeIdAsync)
    /// </summary>
    /// <param name="rangeId">Range ID</param>
    public async Task<IEnumerable<RangeValue>> GetValuesForRangeAsync(string rangeId)
    {
        return await GetValuesByRangeIdAsync(rangeId);
    }
    
    /// <summary>
    /// Gets a range value by name and range ID
    /// </summary>
    /// <param name="name">Value name</param>
    /// <param name="rangeId">Range ID</param>
    public async Task<RangeValue?> GetByNameAndRangeIdAsync(string name, string rangeId)
    {
        return await DbContext.RangeValues
            .FirstOrDefaultAsync(rv => rv.Name == name && rv.RangeId == rangeId);
    }
    
    /// <summary>
    /// Updates a range value
    /// </summary>
    /// <param name="id">Value ID</param>
    /// <param name="name">New name</param>
    /// <param name="value">New value</param>
    public async Task UpdateRangeValueAsync(string id, string name, string value)
    {
        var rangeValue = await DbContext.RangeValues.FindAsync(id);
        if (rangeValue == null)
        {
            throw new EntityNotFoundException("RangeValue", id);
        }
        
        rangeValue.Name = name;
        rangeValue.Value = value;
        
        DbContext.RangeValues.Update(rangeValue);
        await DbContext.SaveChangesAsync();
    }
}