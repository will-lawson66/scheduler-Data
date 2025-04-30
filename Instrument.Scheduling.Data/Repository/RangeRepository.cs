using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Scheduling.Data.Repository;

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
    
    /// <summary>
    /// Gets a range with its values
    /// </summary>
    /// <param name="id">Range ID</param>
    public async Task<Entities.Range?> GetRangeWithValuesAsync(string id)
    {
        return await DbContext.Ranges
            .Include(r => r.Values)
            .FirstOrDefaultAsync(r => r.Id == id);
    }
    
    /// <summary>
    /// Gets ranges by parameter
    /// </summary>
    /// <param name="parameterId">Parameter ID</param>
    public async Task<IEnumerable<Entities.Range>> GetRangesByParameterAsync(string parameterId)
    {
        var parameters = await DbContext.Parameters
            .Where(p => p.Id == parameterId && p.RangeId != null)
            .ToListAsync();
            
        var rangeIds = parameters
            .Where(p => p.RangeId != null)
            .Select(p => p.RangeId)
            .Distinct();
            
        return await DbContext.Ranges
            .Where(r => rangeIds.Contains(r.Id))
            .ToListAsync();
    }
    
    /// <summary>
    /// Adds a range value to a range
    /// </summary>
    /// <param name="rangeId">Range ID</param>
    /// <param name="name">Value name</param>
    /// <param name="value">Value</param>
    public async Task<RangeValue> AddRangeValueAsync(string rangeId, string name, string value)
    {
        // Check if the range exists
        var range = await DbContext.Ranges.FindAsync(rangeId);
        if (range == null)
        {
            throw new EntityNotFoundException("Range", rangeId);
        }
        
        // Check if a range value with the same name already exists
        var existingValue = await DbContext.RangeValues
            .FirstOrDefaultAsync(rv => rv.RangeId == rangeId && rv.Name == name);
            
        if (existingValue != null)
        {
            // TODO: throw an exception?
            // Update the existing value

            existingValue.Value = value;
            DbContext.RangeValues.Update(existingValue);
            await DbContext.SaveChangesAsync();
            return existingValue;
        }
        
        // Create a new range value
        var rangeValue = new RangeValue
        {
            Id = Guid.NewGuid().ToString(),
            RangeId = rangeId,
            Name = name,
            Value = value
        };
        
        await DbContext.RangeValues.AddAsync(rangeValue);
        await DbContext.SaveChangesAsync();
        return rangeValue;
    }
    
    /// <summary>
    /// Removes a range value from a range
    /// </summary>
    /// <param name="rangeValueId">Range value ID</param>
    public async Task RemoveRangeValueAsync(string rangeValueId)
    {
        var rangeValue = await DbContext.RangeValues.FindAsync(rangeValueId);
        if (rangeValue != null)
        {
            DbContext.RangeValues.Remove(rangeValue);
            await DbContext.SaveChangesAsync();
        }
        else
        {
            throw new EntityNotFoundException("RangeValue", rangeValueId);
        }
    }
    
    /// <summary>
    /// Gets parameters for a range
    /// </summary>
    /// <param name="rangeId">Range ID</param>
    public async Task<IEnumerable<Parameter>> GetParametersForRangeAsync(string rangeId)
    {
        return await DbContext.Parameters
            .Where(p => p.RangeId == rangeId)
            .ToListAsync();
    }
}