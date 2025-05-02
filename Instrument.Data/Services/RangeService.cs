using Instrument\.Data.Entities;
using Instrument\.Data.Exceptions;
using Instrument\.Data.Interfaces;
using Microsoft.Extensions.Logging;

namespace Instrument\.Data.Services;

public class RangeService
{
    private readonly IRangeRepository _rangeRepository;
    private readonly ILogger<RangeService> _logger;

    public RangeService(IRangeRepository rangeRepository, ILogger<RangeService> logger)
    {
        _rangeRepository = rangeRepository ?? throw new ArgumentNullException(nameof(rangeRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Entities.Range?> GetRangeAsync(string id)
    {
        _logger.LogInformation("Retrieving range with ID: {Id}", id);
        return await _rangeRepository.GetByIdAsync(id);
    }

    public async Task<Entities.Range?> GetRangeWithValuesAsync(string id)
    {
        _logger.LogInformation("Retrieving range with values for ID: {Id}", id);
        return await _rangeRepository.GetRangeWithValuesAsync(id);
    }

    public async Task CreateRangeAsync(Entities.Range range)
    {
        if (range == null)
            throw new ArgumentNullException(nameof(range));
            
        _logger.LogInformation("Creating new range with ID: {Id}, Name: {Name}", range.Id, range.Name);
        
        await _rangeRepository.AddAsync(range);
        await _rangeRepository.SaveChangesAsync();
        
        _logger.LogInformation("Successfully created range with ID: {Id}", range.Id);
    }

    public async Task UpdateRangeAsync(Entities.Range range)
    {
        if (range == null)
            throw new ArgumentNullException(nameof(range));
            
        _logger.LogInformation("Updating range with ID: {Id}, Name: {Name}", range.Id, range.Name);
        
        await _rangeRepository.UpdateAsync(range);
        await _rangeRepository.SaveChangesAsync();
        
        _logger.LogInformation("Successfully updated range with ID: {Id}", range.Id);
    }
    
    // New property-based update method
    public async Task<Entities.Range> UpdateRangePropertiesAsync(
        string id,
        string? name = null,
        string? description = null)
    {
        _logger.LogInformation("Updating properties for range with ID: {Id}", id);
        
        try
        {
            // Get the current entity
            var range = await _rangeRepository.GetByIdAsync(id);
            if (range == null)
            {
                _logger.LogWarning("Range with ID {Id} does not exist", id);
                throw new EntityNotFoundException("Range", id);
            }
            
            // Use the entity's Update method to create a modified copy
            var updatedRange = range.Update(name, description);
            
            // Use the existing UpdateAsync method
            await _rangeRepository.UpdateAsync(updatedRange);
            await _rangeRepository.SaveChangesAsync();
            
            _logger.LogInformation("Successfully updated properties for range with ID: {Id}", id);
            return updatedRange;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating properties for range with ID: {Id}", id);
            throw new StorageProviderException("UpdateRangeProperties", ex);
        }
    }

    public async Task DeleteRangeAsync(string id)
    {
        _logger.LogInformation("Deleting range with ID: {Id}", id);
        
        await _rangeRepository.DeleteAsync(id);
        await _rangeRepository.SaveChangesAsync();
        
        _logger.LogInformation("Successfully deleted range with ID: {Id}", id);
    }

    public async Task<IEnumerable<Entities.Range>> GetAllRangesAsync()
    {
        _logger.LogInformation("Retrieving all ranges");
        return await _rangeRepository.GetAllAsync();
    }
    
    public async Task<IEnumerable<Parameter>> GetParametersForRangeAsync(string rangeId)
    {
        _logger.LogInformation("Retrieving parameters for range ID: {RangeId}", rangeId);
        return await _rangeRepository.GetParametersForRangeAsync(rangeId);
    }
}
