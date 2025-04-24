using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;

namespace Instrument.Scheduling.Data.Services;

public class ResourceService
{
    private readonly IUnitOfWork _unitOfWork;

    public ResourceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Resource?> GetResourceAsync(string id)
    {
        return await _unitOfWork.Resources.GetByIdAsync(id);
    }

    public async Task CreateResourceAsync(Resource resource)
    {
        await _unitOfWork.Resources.AddAsync(resource);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateResourceAsync(Resource resource)
    {
        await _unitOfWork.Resources.UpdateAsync(resource);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteResourceAsync(string id)
    {
        await _unitOfWork.Resources.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<Resource>> GetAllResourcesAsync()
    {
        return await _unitOfWork.Resources.GetAllAsync();
    }
    
    public async Task<IEnumerable<Parameter>> GetParametersForResourceAsync(string resourceId)
    {
        return await _unitOfWork.Resources.GetParametersForResourceAsync(resourceId);
    }
    
    public async Task<IEnumerable<Resource>> GetAvailableResourcesAsync()
    {
        return await _unitOfWork.Resources.GetAvailableResourcesAsync();
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
