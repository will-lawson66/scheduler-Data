using Instrument.Data.Entities;

namespace Instrument.Data;

public interface IResourceService
{
    Task<Resource?> GetResourceByIdAsync(int id);
    Task CreateResourceAsync(Resource resource);
    Task UpdateResourceAsync(Resource resource);
    Task DeleteResourceAsync(int id);
    Task<IEnumerable<Resource>> GetAllResourcesAsync();
}
