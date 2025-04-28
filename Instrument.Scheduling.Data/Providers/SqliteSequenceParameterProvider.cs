using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Scheduling.Data.Providers;

/// <summary>
/// Provides storage operations for SequenceParameter entities in SQLite
/// </summary>
public class SqliteSequenceParameterProvider : IStorageProvider<SequenceParameter>
{
    private readonly SchedulerDbContext _context;

    public SqliteSequenceParameterProvider(SchedulerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Gets all sequence parameter associations
    /// </summary>
    public async Task<IEnumerable<SequenceParameter>> GetAllAsync()
    {
        try
        {
            return await _context.SequenceParameters
                .Include(sp => sp.Parameter)
                .Include(sp => sp.Sequence)
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception ex)
        {
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
                throw new ArgumentException("Invalid composite ID format. Expected 'sequenceId_parameterId'", nameof(id));

            var parts = id.Split('_', 2);
            if (parts.Length != 2)
                throw new ArgumentException("Invalid composite ID format. Expected 'sequenceId_parameterId'", nameof(id));

            string sequenceId = parts[0];
            string parameterId = parts[1];

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
            throw new StorageProviderException("GetByIdsAsync", ex);
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
                    throw new EntityNotFoundException("Sequence", entity.SequenceId);

                var parameter = await _context.Parameters.FindAsync(entity.ParameterId);
                if (parameter == null)
                    throw new EntityNotFoundException("Parameter", entity.ParameterId);

                await _context.SequenceParameters.AddAsync(entity);
            }
        }
        catch (EntityNotFoundException)
        {
            throw; // Re-throw known entity not found exceptions
        }
        catch (Exception ex)
        {
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
            // Find the entity to update
            var existing = await _context.SequenceParameters
                .FirstOrDefaultAsync(sp =>
                    sp.SequenceId == entity.SequenceId &&
                    sp.ParameterId == entity.ParameterId);

            if (existing == null)
                throw new EntityNotFoundException("SequenceParameter", $"{entity.SequenceId}_{entity.ParameterId}");

            // Detach existing entity and update with new values
            _context.Entry(existing).State = EntityState.Detached;
            _context.SequenceParameters.Update(entity);
        }
        catch (EntityNotFoundException)
        {
            throw; // Re-throw known entity not found exceptions
        }
        catch (Exception ex)
        {
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
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _context.SequenceParameters.Remove(entity);
            }
        }
        catch (ArgumentException)
        {
            throw; // Re-throw argument exceptions
        }
        catch (Exception ex)
        {
            throw new StorageProviderException("DeleteAsync", ex);
        }
    }

    /// <summary>
    /// Saves all pending changes to the database
    /// </summary>
    public async Task SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new StorageProviderException("SaveChangesAsync", ex);
        }
    }
}