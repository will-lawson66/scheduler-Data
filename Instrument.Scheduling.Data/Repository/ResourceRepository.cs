using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Scheduling.Data.Repository;

/// <summary>
/// Repository for resources
/// </summary>
public class ResourceRepository : Repository<Resource>, IResourceRepository
{
    /// <summary>
    /// Creates a new resource repository
    /// </summary>
    /// <param name="dbContext">Database context</param>
    public ResourceRepository(SchedulerDbContext dbContext)
        : base(dbContext)
    {
    }
    
    /// <summary>
    /// Gets a resource by code
    /// </summary>
    /// <param name="code">Resource code</param>
    public async Task<Resource?> GetByCodeAsync(string code)
    {
        return await DbContext.Resources
            .FirstOrDefaultAsync(r => r.Code == code);
    }
    
    /// <summary>
    /// Gets resources with their parameters
    /// </summary>
    public async Task<IEnumerable<Resource>> GetResourcesWithParametersAsync()
    {
        return await DbContext.Resources
            .Include(r => r.Parameters)
            .ToListAsync();
    }
    
    /// <summary>
    /// Adds a parameter to a resource
    /// </summary>
    /// <param name="resourceId">Resource ID</param>
    /// <param name="parameterId">Parameter ID</param>
    public async Task AddParameterToResourceAsync(string resourceId, string parameterId)
    {
        // Check if the resource exists
        var resource = await DbContext.Resources.FindAsync(resourceId);
        if (resource == null)
        {
            throw new EntityNotFoundException("Resource", resourceId);
        }
        
        // Check if the parameter exists
        var parameter = await DbContext.Parameters.FindAsync(parameterId);
        if (parameter == null)
        {
            throw new EntityNotFoundException("Parameter", parameterId);
        }
        
        // Update the parameter to associate it with the resource
        parameter.ResourceId = resourceId;
        
        DbContext.Parameters.Update(parameter);
        await DbContext.SaveChangesAsync();
    }
    
    /// <summary>
    /// Removes a parameter from a resource
    /// </summary>
    /// <param name="resourceId">Resource ID</param>
    /// <param name="parameterId">Parameter ID</param>
    public async Task RemoveParameterFromResourceAsync(string resourceId, string parameterId)
    {
        // Find the parameter
        var parameter = await DbContext.Parameters
            .FirstOrDefaultAsync(p => p.Id == parameterId && p.ResourceId == resourceId);
            
        if (parameter == null)
        {
            throw new EntityNotFoundException("Parameter", parameterId);
        }
        
        // Remove the association
        parameter.ResourceId = null;
        
        DbContext.Parameters.Update(parameter);
        await DbContext.SaveChangesAsync();
    }
    
    /// <summary>
    /// Gets parameters for a resource
    /// </summary>
    /// <param name="resourceId">Resource ID</param>
    public async Task<IEnumerable<Parameter>> GetParametersForResourceAsync(string resourceId)
    {
        return await DbContext.Parameters
            .Where(p => p.ResourceId == resourceId)
            .ToListAsync();
    }
}