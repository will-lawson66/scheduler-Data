using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Instrument.Scheduling.Data.Providers;

/// <summary>
/// Provides storage operations for SequenceParameter entities in SQL Server
/// </summary>
public class SqlServerSequenceParameterProvider : IStorageProvider<SequenceParameter>
{
    private readonly SchedulerDbContext _context;
    private readonly ILogger<SqlServerSequenceParameterProvider> _logger;

    public SqlServerSequenceParameterProvider(
        SchedulerDbContext context,
        ILogger<SqlServerSequenceParameterProvider> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all sequence parameter associations
    /// </summary>
    public async Task<IEnumerable<SequenceParameter>> GetAllAsync()
    {
        try
        {
            _logger.LogDebug("Retrieving all sequence parameters");
            return await _context.SequenceParameters
                .Include(sp => sp.Parameter)
                .Include(sp => sp.Sequence)
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all sequence parameters");
            throw new StorageProviderException("GetAllAsync", ex);
        }
    }

    /// <summary>
    /// Gets a sequence parameter by its composite ID
    /// </summary>
    /// <param name="id">Composite ID in the format "sequenceId_parameterId"</param>
    public async Task<SequenceParameter?> GetByIdAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentNullException(nameof(id));

        try
        {
            // The ID for a junction entity is a composite key formatted as "sequenceId_parameterId"
            if (!id.Contains('_'))
            {
                _logger.LogWarning("Invalid composite ID format: {Id}", id);
                throw new ArgumentException("Invalid composite ID format. Expected 'sequenceId_parameterId'", nameof(id));
            }

            var parts = id.Split('_', 2);
            if (parts.Length != 2)
            {
                _logger.LogWarning("Invalid composite ID format: {Id}", id);
                throw new ArgumentException("Invalid composite ID format. Expected 'sequenceId_parameterId'", nameof(id));
            }

            string sequenceId = parts[0];
            string parameterId = parts[1];

            _logger.LogDebug("Retrieving sequence parameter with sequenceId {SequenceId} and parameterId {ParameterId}",
                sequenceId, parameterId);

            return await _context.SequenceParameters
                .Include(sp => sp.Parameter)
                .Include(sp => sp.Sequence)
                .AsNoTracking()
                .FirstOrDefaultAsync(sp =>
                    sp.SequenceId == sequenceId &&
                    sp.ParameterId == parameterId);
        }
        catch (ArgumentException)
        {
            throw; // Re-throw argument exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sequence parameter with ID {Id}", id);
            throw new StorageProviderException("GetByIdAsync", ex);
        }
    }

    /// <summary>
    /// Gets a sequence parameter by its constituent IDs
    /// </summary>
    public async Task<SequenceParameter?> GetByIdsAsync(string sequenceId, string parameterId)
    {
        if (string.IsNullOrEmpty(sequenceId))
            throw new ArgumentNullException(nameof(sequenceId));
        if (string.IsNullOrEmpty(parameterId))
            throw new ArgumentNullException(nameof(parameterId));

        try
        {
            _logger.LogDebug("Retrieving sequence parameter with sequenceId {SequenceId} and parameterId {ParameterId}",
                sequenceId, parameterId);

            return await _context.SequenceParameters
                .Include(sp => sp.Parameter)
                .Include(sp => sp.Sequence)
                .AsNoTracking()
                .FirstOrDefaultAsync(sp =>
                    sp.SequenceId == sequenceId &&
                    sp.ParameterId == parameterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sequence parameter with sequenceId {SequenceId} and parameterId {ParameterId}",
                sequenceId, parameterId);
            throw new StorageProviderException("GetByIdsAsync", ex);
        }
    }

    /// <summary>
    /// Gets all sequence parameters for a given sequence
    /// </summary>
    public async Task<IEnumerable<SequenceParameter>> GetForSequenceAsync(string sequenceId)
    {
        if (string.IsNullOrEmpty(sequenceId))
            throw new ArgumentNullException(nameof(sequenceId));

        try
        {
            _logger.LogDebug("Retrieving sequence parameters for sequence {SequenceId}", sequenceId);

            return await _context.SequenceParameters
                .Include(sp => sp.Parameter)
                .Where(sp => sp.SequenceId == sequenceId)
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sequence parameters for sequence {SequenceId}", sequenceId);
            throw new StorageProviderException("GetForSequenceAsync", ex);
        }
    }

    /// <summary>
    /// Gets all sequence parameters for a given parameter
    /// </summary>
    public async Task<IEnumerable<SequenceParameter>> GetForParameterAsync(string parameterId)
    {
        if (string.IsNullOrEmpty(parameterId))
            throw new ArgumentNullException(nameof(parameterId));

        try
        {
            _logger.LogDebug("Retrieving sequence parameters for parameter {ParameterId}", parameterId);

            return await _context.SequenceParameters
                .Include(sp => sp.Sequence)
                .Where(sp => sp.ParameterId == parameterId)
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sequence parameters for parameter {ParameterId}", parameterId);
            throw new StorageProviderException("GetForParameterAsync", ex);
        }
    }

    /// <summary>
    /// Adds a sequence parameter association
    /// </summary>
    public async Task AddAsync(SequenceParameter entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            _logger.LogDebug("Adding sequence parameter: Sequence {SequenceId}, Parameter {ParameterId}",
                entity.SequenceId, entity.ParameterId);

            // Check if it already exists
            var existing = await _context.SequenceParameters
                .FirstOrDefaultAsync(sp =>
                    sp.SequenceId == entity.SequenceId &&
                    sp.ParameterId == entity.ParameterId);

            if (existing == null)
            {
                // Verify the sequence and parameter exist
                var sequence = await _context.Sequences.FindAsync(entity.SequenceId);
                if (sequence == null)
                {
                    _logger.LogWarning("Sequence with ID {SequenceId} not found when adding sequence parameter", entity.SequenceId);
                    throw new EntityNotFoundException("Sequence", entity.SequenceId);
                }

                var parameter = await _context.Parameters.FindAsync(entity.ParameterId);
                if (parameter == null)
                {
                    _logger.LogWarning("Parameter with ID {ParameterId} not found when adding sequence parameter", entity.ParameterId);
                    throw new EntityNotFoundException("Parameter", entity.ParameterId);
                }

                await _context.SequenceParameters.AddAsync(entity);
                _logger.LogInformation("Added sequence parameter: Sequence {SequenceId}, Parameter {ParameterId}",
                    entity.SequenceId, entity.ParameterId);
            }
            else
            {
                _logger.LogInformation("Sequence parameter already exists: Sequence {SequenceId}, Parameter {ParameterId}",
                    entity.SequenceId, entity.ParameterId);
            }
        }
        catch (EntityNotFoundException)
        {
            throw; // Re-throw known entity not found exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add sequence parameter: Sequence {SequenceId}, Parameter {ParameterId}",
                entity.SequenceId, entity.ParameterId);
            throw new StorageProviderException("AddAsync", ex);
        }
    }

    /// <summary>
    /// Updates a sequence parameter association
    /// </summary>
    public async Task UpdateAsync(SequenceParameter entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            _logger.LogDebug("Updating sequence parameter: Sequence {SequenceId}, Parameter {ParameterId}",
                entity.SequenceId, entity.ParameterId);

            // Find the entity to update
            var existing = await _context.SequenceParameters
                .FirstOrDefaultAsync(sp =>
                    sp.SequenceId == entity.SequenceId &&
                    sp.ParameterId == entity.ParameterId);

            if (existing == null)
            {
                _logger.LogWarning("Sequence parameter not found for update: Sequence {SequenceId}, Parameter {ParameterId}",
                    entity.SequenceId, entity.ParameterId);
                throw new EntityNotFoundException("SequenceParameter", $"{entity.SequenceId}_{entity.ParameterId}");
            }

            // Only update OrderNumber as the primary keys can't be changed
            _context.Entry(existing).State = EntityState.Detached;
            _context.SequenceParameters.Update(entity);
            _logger.LogInformation("Updated sequence parameter: Sequence {SequenceId}, Parameter {ParameterId}",
                entity.SequenceId, entity.ParameterId);
        }
        catch (EntityNotFoundException)
        {
            throw; // Re-throw known entity not found exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update sequence parameter: Sequence {SequenceId}, Parameter {ParameterId}",
                entity.SequenceId, entity.ParameterId);
            throw new StorageProviderException("UpdateAsync", ex);
        }
    }

    /// <summary>
    /// Deletes a sequence parameter association by its composite ID
    /// </summary>
    public async Task DeleteAsync(string id)
    {
        try
        {
            _logger.LogDebug("Deleting sequence parameter with ID {Id}", id);

            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _context.SequenceParameters.Remove(entity);
                _logger.LogInformation("Deleted sequence parameter: Sequence {SequenceId}, Parameter {ParameterId}",
                    entity.SequenceId, entity.ParameterId);
            }
            else
            {
                _logger.LogWarning("Sequence parameter not found for deletion: {Id}", id);
            }
        }
        catch (ArgumentException)
        {
            throw; // Re-throw argument exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete sequence parameter: {Id}", id);
            throw new StorageProviderException("DeleteAsync", ex);
        }
    }

    /// <summary>
    /// Deletes a sequence parameter association by its constituent IDs
    /// </summary>
    public async Task DeleteByIdsAsync(string sequenceId, string parameterId)
    {
        if (string.IsNullOrEmpty(sequenceId))
            throw new ArgumentNullException(nameof(sequenceId));
        if (string.IsNullOrEmpty(parameterId))
            throw new ArgumentNullException(nameof(parameterId));

        try
        {
            _logger.LogDebug("Deleting sequence parameter with sequenceId {SequenceId} and parameterId {ParameterId}",
                sequenceId, parameterId);

            var entity = await GetByIdsAsync(sequenceId, parameterId);
            if (entity != null)
            {
                _context.SequenceParameters.Remove(entity);
                _logger.LogInformation("Deleted sequence parameter: Sequence {SequenceId}, Parameter {ParameterId}",
                    sequenceId, parameterId);
            }
            else
            {
                _logger.LogWarning("Sequence parameter not found for deletion: Sequence {SequenceId}, Parameter {ParameterId}",
                    sequenceId, parameterId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete sequence parameter with sequenceId {SequenceId} and parameterId {ParameterId}",
                sequenceId, parameterId);
            throw new StorageProviderException("DeleteByIdsAsync", ex);
        }
    }

    /// <summary>
    /// Saves all pending changes to the database
    /// </summary>
    public async Task SaveChangesAsync()
    {
        try
        {
            _logger.LogDebug("Saving changes to the database");
            await _context.SaveChangesAsync();
            _logger.LogInformation("Saved changes to the database");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict when saving changes");
            throw new StorageProviderException("SaveChangesAsync (Concurrency conflict)", ex);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error when saving changes");
            throw new StorageProviderException("SaveChangesAsync (Update error)", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save changes to the database");
            throw new StorageProviderException("SaveChangesAsync", ex);
        }
    }
}
