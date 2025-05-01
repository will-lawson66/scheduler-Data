using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;

namespace Instrument.Scheduling.Data.Services;

public class ResourceService
{
    private readonly IResourceRepository _resourceRepository;

    public ResourceService(IResourceRepository resourceRepository)
    {
        _resourceRepository = resourceRepository;
    }

    public async Task<Resource?> GetResourceAsync(string id)
    {
        return await _resourceRepository.GetByIdAsync(id);
    }

    public async Task CreateResourceAsync(Resource resource)
    {
        await _resourceRepository.AddAsync(resource);
        await _resourceRepository.SaveChangesAsync();
    }

    public async Task UpdateResourceAsync(Resource resource)
    {
        await _resourceRepository.UpdateAsync(resource);
        await _resourceRepository.SaveChangesAsync();
    }

    public async Task DeleteResourceAsync(string id)
    {
        await _resourceRepository.DeleteAsync(id);
        await _resourceRepository.SaveChangesAsync();
    }

    public async Task<IEnumerable<Resource>> GetAllResourcesAsync()
    {
        return await _resourceRepository.GetAllAsync();
    }
    
    public async Task<IEnumerable<Parameter>> GetParametersForResourceAsync(string resourceId)
    {
        return await _resourceRepository.GetParametersForResourceAsync(resourceId);
    }
    
    // Lock a resource
    public async Task LockResourceAsync(string id)
    {
        var resource = await GetResourceAsync(id);
        if (resource != null)
        {
            var updatedResource = resource with { Locked = true };
            await UpdateResourceAsync(updatedResource);
        }
    }
    
    // Unlock a resource
    public async Task UnlockResourceAsync(string id)
    {
        var resource = await GetResourceAsync(id);
        if (resource != null)
        {
            var updatedResource = resource with { Locked = false };
            await UpdateResourceAsync(updatedResource);
        }
    }
}
