namespace Instrument.Data;

/// <summary>
/// Repository interface for resources
/// </summary>
public interface IResourceRepository : IRepository<Instrument.Data.Entities.Resource>
{
    /// <summary>
    /// Gets a resource by code
    /// </summary>
    /// <param name="code">Resource code</param>
    Task<Instrument.Data.Entities.Resource?> GetByCodeAsync(string code);
    
    /// <summary>
    /// Gets resources with their parameters
    /// </summary>
    Task<IEnumerable<Instrument.Data.Entities.Resource>> GetResourcesWithParametersAsync();
    
    /// <summary>
    /// Adds a parameter to a resource
    /// </summary>
    /// <param name="resourceId">Resource ID</param>
    /// <param name="parameterId">Parameter ID</param>
    Task AddParameterToResourceAsync(string resourceId, string parameterId);
    
    /// <summary>
    /// Removes a parameter from a resource
    /// </summary>
    /// <param name="resourceId">Resource ID</param>
    /// <param name="parameterId">Parameter ID</param>
    Task RemoveParameterFromResourceAsync(string resourceId, string parameterId);
    
    /// <summary>
    /// Gets parameters for a resource
    /// </summary>
    /// <param name="resourceId">Resource ID</param>
    Task<IEnumerable<Instrument.Data.Entities.Parameter>> GetParametersForResourceAsync(string resourceId);
}