using Instrument.Data.Exceptions;
using Microsoft.Extensions.Logging;

namespace Instrument.Data.Services;

public class RangeService : IRangeService
{
    private readonly IRangeRepository _rangeRepository;
    private readonly ILogger<RangeService> _logger;

    public RangeService(IRangeRepository rangeRepository, ILogger<RangeService> logger)
    {
        _rangeRepository = rangeRepository ?? throw new ArgumentNullException(nameof(rangeRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Entities.Range?> GetRangeByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving range with ID: {Id}", id);
        return await _rangeRepository.GetByIdAsync(id);
    }

    public async Task<Entities.Range?> GetRangeWithRangeValuesAsync(int id)
    {
        _logger.LogInformation("Retrieving range with values for ID: {Id}", id);
        return await _rangeRepository.GetRangeWithRangeValuesByIdAsync(id);
    }

    public async Task<Entities.Range> CreateRangeAsync(Entities.Range range)
    {
        if (range == null)
        {
            throw new ArgumentNullException(nameof(range));
        }
            
        _logger.LogInformation("Creating new range with Name: {Name}", range.Name);

        try
        {
            await _rangeRepository.AddAsync(range);
            await _rangeRepository.SaveChangesAsync(); 
            _logger.LogInformation("Successfully created range with ID: {Id}", range.Id);
            return range;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Range with Name: {Name}:", range.Name);
            throw new StorageProviderException("CreateRange", ex);
        }
    }

    public async Task UpdateRangeAsync(Entities.Range range)
    {
        if (range == null)
        {
            throw new ArgumentNullException(nameof(range));
        }
            
        _logger.LogInformation("Updating range with ID: {Id}, Name: {Name}", range.Id, range.Name);
        
        await _rangeRepository.UpdateAsync(range);
        await _rangeRepository.SaveChangesAsync();
        
        _logger.LogInformation("Successfully updated range with ID: {Id}", range.Id);
    }
    
    public async Task DeleteRangeAsync(int id)
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
}
