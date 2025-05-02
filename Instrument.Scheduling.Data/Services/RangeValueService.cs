using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Interfaces;
using Microsoft.Extensions.Logging;

namespace Instrument.Scheduling.Data.Services;

public class RangeValueService
{
    private readonly IRangeValueRepository _rangeValueRepository;
    private readonly ILogger<RangeValueService> _logger;

    public RangeValueService(IRangeValueRepository rangeValueRepository, ILogger<RangeValueService> logger)
    {
        _rangeValueRepository = rangeValueRepository ?? throw new ArgumentNullException(nameof(rangeValueRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RangeValue?> GetRangeValueAsync(string id)
    {
        _logger.LogInformation("Retrieving range value with ID: {Id}", id);
        return await _rangeValueRepository.GetByIdAsync(id);
    }

    public async Task CreateRangeValueAsync(RangeValue rangeValue)
    {
        if (rangeValue == null)
            throw new ArgumentNullException(nameof(rangeValue));
            
        _logger.LogInformation("Creating new range value with ID: {Id}, Name: {Name}", rangeValue.Id, rangeValue.Name);
        
        await _rangeValueRepository.AddAsync(rangeValue);
        await _rangeValueRepository.SaveChangesAsync();
        
        _logger.LogInformation("Successfully created range value with ID: {Id}", rangeValue.Id);
    }

    public async Task UpdateRangeValueAsync(RangeValue rangeValue)
    {
        if (rangeValue == null)
            throw new ArgumentNullException(nameof(rangeValue));
            
        _logger.LogInformation("Updating range value with ID: {Id}, Name: {Name}", rangeValue.Id, rangeValue.Name);
        
        await _rangeValueRepository.UpdateAsync(rangeValue);
        await _rangeValueRepository.SaveChangesAsync();
        
        _logger.LogInformation("Successfully updated range value with ID: {Id}", rangeValue.Id);
    }
    
    // New property-based update method
    public async Task<RangeValue> UpdateRangeValuePropertiesAsync(
        string id,
        string? name = null,
        string? value = null)
    {
        _logger.LogInformation("Updating properties for range value with ID: {Id}", id);
        
        try
        {
            // Get the current entity
            var rangeValue = await _rangeValueRepository.GetByIdAsync(id);
            if (rangeValue == null)
            {
                _logger.LogWarning("Range value with ID {Id} does not exist", id);
                throw new EntityNotFoundException("RangeValue", id);
            }
            
            // Use the entity's Update method to create a modified copy
            var updatedRangeValue = rangeValue.Update(name, value);
            
            // Use the existing UpdateAsync method
            await _rangeValueRepository.UpdateAsync(updatedRangeValue);
            await _rangeValueRepository.SaveChangesAsync();
            
            _logger.LogInformation("Successfully updated properties for range value with ID: {Id}", id);
            return updatedRangeValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating properties for range value with ID: {Id}", id);
            throw new StorageProviderException("UpdateRangeValueProperties", ex);
        }
    }

    public async Task DeleteRangeValueAsync(string id)
    {
        _logger.LogInformation("Deleting range value with ID: {Id}", id);
        
        await _rangeValueRepository.DeleteAsync(id);
        await _rangeValueRepository.SaveChangesAsync();
        
        _logger.LogInformation("Successfully deleted range value with ID: {Id}", id);
    }

    public async Task<IEnumerable<RangeValue>> GetAllRangeValuesAsync()
    {
        _logger.LogInformation("Retrieving all range values");
        return await _rangeValueRepository.GetAllAsync();
    }
    
    public async Task<IEnumerable<RangeValue>> GetValuesForRangeAsync(string rangeId)
    {
        _logger.LogInformation("Retrieving values for range ID: {RangeId}", rangeId);
        return await _rangeValueRepository.GetValuesForRangeAsync(rangeId);
    }
    
    // Check if a value is valid for a range
    public async Task<bool> IsValueValidForRangeAsync(string rangeId, string value)
    {
        _logger.LogInformation("Checking if value '{Value}' is valid for range ID: {RangeId}", value, rangeId);
        var rangeValues = await GetValuesForRangeAsync(rangeId);
        return rangeValues.Any(rv => rv.Value == value);
    }
}
