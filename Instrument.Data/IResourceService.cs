using Instrument.Data.Entities;

namespace Instrument.Data;

public interface IResourceService
{
    Task<Resource?> GetResourceByIdAsync(int id);
    Task<Resource> CreateResourceAsync(Resource resource);
    Task UpdateResourceAsync(Resource resource);
    Task DeleteResourceAsync(int id);
    Task<IEnumerable<Resource>> GetAllResourcesAsync();
    
    /// <summary>
    /// Gets a resource by code
    /// </summary>
    /// <param name="code">Resource code</param>
    Task<Resource?> GetByCodeAsync(string code);

    /// <summary>
    /// Gets resources with their parameters
    /// </summary>
    Task<IEnumerable<Resource>> GetResourcesWithParametersAsync();

    /// <summary>
    /// Adds a parameter to a resource
    /// </summary>
    /// <param name="resourceId">Resource ID</param>
    /// <param name="parameterId">Parameter ID</param>
    Task AddParameterToResourceAsync(int resourceId, int parameterId);

    /// <summary>
    /// Removes a parameter from a resource
    /// </summary>
    /// <param name="resourceId">Resource ID</param>
    /// <param name="parameterId">Parameter ID</param>
    Task RemoveParameterFromResourceAsync(int resourceId, int parameterId);

    /// <summary>
    /// Gets parameters for a resource
    /// </summary>
    /// <param name="resourceId">Resource ID</param>
    Task<IEnumerable<Parameter>> GetParametersForResourceAsync(int resourceId);
}
