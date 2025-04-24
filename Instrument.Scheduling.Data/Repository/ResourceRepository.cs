using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;

namespace Instrument.Scheduling.Data.Repository;

public class ResourceRepository : IResourceRepository
{
    private readonly IStorageProvider<Resource> _resourceStorageProvider;
    private readonly IStorageProvider<Parameter> _parameterStorageProvider;

    public ResourceRepository(
        IStorageProvider<Resource> resourceStorageProvider,
        IStorageProvider<Parameter> parameterStorageProvider)
    {
        _resourceStorageProvider = resourceStorageProvider;
        _parameterStorageProvider = parameterStorageProvider;
    }

    public async Task<IEnumerable<Resource>> GetAllAsync()
    {
        return await _resourceStorageProvider.GetAllAsync();
    }

    public async Task<Resource?> GetByIdAsync(string id)
    {
        return await _resourceStorageProvider.GetByIdAsync(id);
    }

    public async Task<IQueryable<Resource>> GetQueryableAsync()
    {
        var data = await _resourceStorageProvider.GetAllAsync();
        return data.AsQueryable();
    }

    public async Task AddAsync(Resource resource)
    {
        await _resourceStorageProvider.AddAsync(resource);
    }

    public async Task UpdateAsync(Resource resource)
    {
        await _resourceStorageProvider.UpdateAsync(resource);
    }

    public async Task DeleteAsync(string id)
    {
        await _resourceStorageProvider.DeleteAsync(id);
    }
    
    public async Task<IEnumerable<Parameter>> GetParametersForResourceAsync(string resourceId)
    {
        var parameters = await _parameterStorageProvider.GetAllAsync();
        return parameters.Where(p => p.ResourceId == resourceId);
    }
    
    public async Task<IEnumerable<Resource>> GetAvailableResourcesAsync()
    {
        var resources = await _resourceStorageProvider.GetAllAsync();
        return resources.Where(r => !r.Locked);
    }

    public async Task SaveChangesAsync()
    {
        await _resourceStorageProvider.SaveChangesAsync();
    }
}
