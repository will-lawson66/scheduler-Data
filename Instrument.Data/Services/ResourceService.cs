using Instrument.Data.Entities;
using Instrument.Data.Exceptions;
using Instrument.Data.Interfaces;
using Microsoft.Extensions.Logging;

namespace Instrument.Data.Services;

public class ResourceService
{
    private readonly IResourceRepository _resourceRepository;
    private readonly ILogger<ResourceService> _logger;

    public ResourceService(IResourceRepository resourceRepository, ILogger<ResourceService> logger)
    {
        _resourceRepository = resourceRepository ?? throw new ArgumentNullException(nameof(resourceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Resource?> GetResourceAsync(string id)
    {
        _logger.LogInformation("Retrieving resource with ID: {Id}", id);
        return await _resourceRepository.GetByIdAsync(id);
    }

    public async Task CreateResourceAsync(Resource resource)
    {
        if (resource == null)
            throw new ArgumentNullException(nameof(resource));
            
        _logger.LogInformation("Creating new resource with ID: {Id}, Name: {Name}", resource.Id, resource.Name);
        
        await _resourceRepository.AddAsync(resource);
        await _resourceRepository.SaveChangesAsync();
        
        _logger.LogInformation("Successfully created resource with ID: {Id}", resource.Id);
    }

    public async Task UpdateResourceAsync(Resource resource)
    {
        if (resource == null)
            throw new ArgumentNullException(nameof(resource));
            
        _logger.LogInformation("Updating resource with ID: {Id}, Name: {Name}", resource.Id, resource.Name);
        
        await _resourceRepository.UpdateAsync(resource);
        await _resourceRepository.SaveChangesAsync();
        
        _logger.LogInformation("Successfully updated resource with ID: {Id}", resource.Id);
    }
    
    // New property-based update method
    public async Task<Resource> UpdateResourcePropertiesAsync(
        string id,
        string? name = null,
        string? code = null,
        bool? locked = null)
    {
        _logger.LogInformation("Updating properties for resource with ID: {Id}", id);
        
        try
        {
            // Get the current entity
            var resource = await _resourceRepository.GetByIdAsync(id);
            if (resource == null)
            {
                _logger.LogWarning("Resource with ID {Id} does not exist", id);
                throw new EntityNotFoundException("Resource", id);
            }
            
            // Use the entity's Update method to create a modified copy
            var updatedResource = resource.Update(name, code, locked);
            
            // Use the existing UpdateAsync method
            await _resourceRepository.UpdateAsync(updatedResource);
            await _resourceRepository.SaveChangesAsync();
            
            _logger.LogInformation("Successfully updated properties for resource with ID: {Id}", id);
            return updatedResource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating properties for resource with ID: {Id}", id);
            throw new StorageProviderException("UpdateResourceProperties", ex);
        }
    }

    public async Task DeleteResourceAsync(string id)
    {
        _logger.LogInformation("Deleting resource with ID: {Id}", id);
        
        await _resourceRepository.DeleteAsync(id);
        await _resourceRepository.SaveChangesAsync();
        
        _logger.LogInformation("Successfully deleted resource with ID: {Id}", id);
    }

    public async Task<IEnumerable<Resource>> GetAllResourcesAsync()
    {
        _logger.LogInformation("Retrieving all resources");
        return await _resourceRepository.GetAllAsync();
    }
    
    public async Task<IEnumerable<Parameter>> GetParametersForResourceAsync(string resourceId)
    {
        _logger.LogInformation("Retrieving parameters for resource ID: {ResourceId}", resourceId);
        return await _resourceRepository.GetParametersForResourceAsync(resourceId);
    }
    
    // Lock a resource - use the Update method instead of direct property modification
    public async Task LockResourceAsync(string id)
    {
        _logger.LogInformation("Locking resource with ID: {Id}", id);
        await UpdateResourcePropertiesAsync(id, locked: true);
    }
    
    // Unlock a resource - use the Update method instead of direct property modification
    public async Task UnlockResourceAsync(string id)
    {
        _logger.LogInformation("Unlocking resource with ID: {Id}", id);
        await UpdateResourcePropertiesAsync(id, locked: false);
    }
}
