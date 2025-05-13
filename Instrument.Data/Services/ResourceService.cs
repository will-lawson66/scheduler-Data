using Instrument.Data.Entities;
using Microsoft.Extensions.Logging;

namespace Instrument.Data.Services;

public class ResourceService : IResourceService
{
    private readonly IResourceRepository _resourceRepository;
    private readonly ILogger<ResourceService> _logger;

    public ResourceService(IResourceRepository resourceRepository, ILogger<ResourceService> logger)
    {
        _resourceRepository = resourceRepository ?? throw new ArgumentNullException(nameof(resourceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Resource?> GetResourceByIdAsync(string id)
    {
        _logger.LogInformation("Retrieving resource with ID: {Id}", id);
        return await _resourceRepository.GetByIdAsync(id);
    }

    public async Task CreateResourceAsync(Resource resource)
    {
        if (resource == null)
        {
            throw new ArgumentNullException(nameof(resource));
        }
            
        _logger.LogInformation("Creating new resource with ID: {Id}, Name: {Name}", resource.Id, resource.Name);
        
        await _resourceRepository.AddAsync(resource);
        await _resourceRepository.SaveChangesAsync();
        
        _logger.LogInformation("Successfully created resource with ID: {Id}", resource.Id);
    }

    public async Task UpdateResourceAsync(Resource resource)
    {
        if (resource == null)
        {
            throw new ArgumentNullException(nameof(resource));
        }
            
        _logger.LogInformation("Updating resource with ID: {Id}, Name: {Name}", resource.Id, resource.Name);
        
        await _resourceRepository.UpdateAsync(resource);
        await _resourceRepository.SaveChangesAsync();
        
        _logger.LogInformation("Successfully updated resource with ID: {Id}", resource.Id);
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
}
