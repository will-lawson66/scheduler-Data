using Instrument.Data.Entities;
using Instrument.Data.Exceptions;
using Instrument.Data.Repository;
using Microsoft.Extensions.Logging;
using System;

namespace Instrument.Data.Services;

public class RangeValueService : IRangeValueService
{
    private readonly IRangeValueRepository _rangeValueRepository;
    private readonly ILogger<RangeValueService> _logger;

    public RangeValueService(IRangeValueRepository rangeValueRepository, ILogger<RangeValueService> logger)
    {
        _rangeValueRepository = rangeValueRepository ?? throw new ArgumentNullException(nameof(rangeValueRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RangeValue?> GetRangeValueByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving range value with ID: {Id}", id);
        return await _rangeValueRepository.GetByIdAsync(id);
    }

    public async Task<RangeValue> CreateRangeValueAsync(RangeValue rangeValue)
    {
        if (rangeValue == null)
        {
            throw new ArgumentNullException(nameof(rangeValue));
        }
            
        _logger.LogInformation("Creating new range value with Name: {Name}", rangeValue.Name);

        try
        {
            await _rangeValueRepository.AddAsync(rangeValue);
            await _rangeValueRepository.SaveChangesAsync();
            _logger.LogInformation("Successfully created range with ID: {Id}", rangeValue.Id);
            return rangeValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating RangeValue with Name: {Name}:", rangeValue.Name);
            throw new StorageProviderException("CreateRangeValue", ex);
        }
    }

    public async Task UpdateRangeValueAsync(RangeValue rangeValue)
    {
        if (rangeValue == null)
        {
            throw new ArgumentNullException(nameof(rangeValue));
        }
            
        _logger.LogInformation("Updating range value with ID: {Id}, Name: {Name}", rangeValue.Id, rangeValue.Name);
        
        await _rangeValueRepository.UpdateAsync(rangeValue);
        await _rangeValueRepository.SaveChangesAsync();
        
        _logger.LogInformation("Successfully updated range value with ID: {Id}", rangeValue.Id);
    }
    
    public async Task DeleteRangeValueAsync(int id)
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

    /// <inheritdoc/>
    public async Task<IEnumerable<RangeValue>> GetRangeValuesForRangeAsync(int rangeId)
    {
        _logger.LogInformation("Retrieving values for range ID: {RangeId}", rangeId);
        return await _rangeValueRepository.GetRangeValuesByRangeIdAsync(rangeId);
    }
}
