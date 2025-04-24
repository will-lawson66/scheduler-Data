using Instrument.Scheduling.Data.Entities;

namespace Instrument.Scheduling.Data.Interfaces;

public interface IResourceRepository
{
    Task<IEnumerable<Resource>> GetAllAsync();
    Task<Resource?> GetByIdAsync(string id);
    Task<IQueryable<Resource>> GetQueryableAsync();
    Task AddAsync(Resource resource);
    Task UpdateAsync(Resource resource);
    Task DeleteAsync(string id);
    
    // Get all parameters associated with a resource
    Task<IEnumerable<Parameter>> GetParametersForResourceAsync(string resourceId);
    
    // Get available (non-locked) resources
    Task<IEnumerable<Resource>> GetAvailableResourcesAsync();
    
    Task SaveChangesAsync();
}
