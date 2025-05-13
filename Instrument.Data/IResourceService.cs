using Instrument.Data.Entities;

namespace Instrument.Data;

public interface IResourceService
{
    Task<Resource?> GetResourceByIdAsync(string id);
    Task CreateResourceAsync(Resource resource);
    Task UpdateResourceAsync(Resource resource);
    Task DeleteResourceAsync(string id);
    Task<IEnumerable<Resource>> GetAllResourcesAsync();
}
