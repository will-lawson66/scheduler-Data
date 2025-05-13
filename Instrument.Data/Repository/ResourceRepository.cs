using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Instrument.Data.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Data.Repository;

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

    /// <inheritdoc />
    public async Task<Resource?> GetByCodeAsync(string code)
    {
        return await DbContext.Resources
            .FirstOrDefaultAsync(r => r.Code == code);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Resource>> GetResourcesWithParametersAsync()
    {
        return await DbContext.Resources
            .Include(r => r.Parameters)
            .ToListAsync();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<IEnumerable<Parameter>> GetParametersForResourceAsync(string resourceId)
    {
        return await DbContext.Parameters
            .Where(p => p.ResourceId == resourceId)
            .ToListAsync();
    }
    
    /// <summary>
    /// Gets available resources (not locked)
    /// </summary>
    public async Task<IEnumerable<Resource>> GetAvailableResourcesAsync()
    {
        return await DbContext.Resources
            .Where(r => !r.Locked)
            .ToListAsync();
    }
}