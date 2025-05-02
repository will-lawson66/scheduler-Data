using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Scheduling.Data.Repository;

/// <summary>
/// Repository for sequences
/// </summary>
public class SequenceRepository : Repository<Sequence>, ISequenceRepository
{
    /// <summary>
    /// Creates a new sequence repository
    /// </summary>
    /// <param name="dbContext">Database context</param>
    public SequenceRepository(SchedulerDbContext dbContext)
        : base(dbContext)
    {
    }
    
    /// <summary>
    /// Gets a sequence with its parameters
    /// </summary>
    /// <param name="id">Sequence ID</param>
    public async Task<Sequence?> GetSequenceWithParametersAsync(string id)
    {
        return await DbContext.Sequences
            .Include(s => s.SequenceParameters)
            .ThenInclude(sp => sp.Parameter)
            .FirstOrDefaultAsync(s => s.Id == id);
    }
    
    /// <summary>
    /// Gets sequences by name
    /// </summary>
    /// <param name="name">Sequence name</param>
    public async Task<IEnumerable<Sequence>> GetSequencesByNameAsync(string name)
    {
        return await DbContext.Sequences
            .Where(s => s.Name.Contains(name))
            .ToListAsync();
    }
    
    /// <summary>
    /// Removes a parameter from a sequence
    /// </summary>
    /// <param name="parameterId">Parameter ID</param>
    /// <param name="sequenceId">Sequence ID</param>
    public async Task RemoveParameterFromSequenceAsync(string parameterId, string sequenceId)
    {
        var sequenceParameter = await DbContext.SequenceParameters
            .FirstOrDefaultAsync(sp => sp.ParameterId == parameterId && sp.SequenceId == sequenceId);
            
        if (sequenceParameter != null)
        {
            DbContext.SequenceParameters.Remove(sequenceParameter);
            await DbContext.SaveChangesAsync();
        }
    }
    
    /// <summary>
    /// Gets sequences by their IDs
    /// </summary>
    /// <param name="ids">Sequence IDs</param>
    public async Task<IEnumerable<Sequence>> GetSequencesByIdsAsync(IEnumerable<string> ids)
    {
        return await DbContext.Sequences
            .Where(s => ids.Contains(s.Id))
            .ToListAsync();
    }
    
    /// <summary>
    /// Updates a sequence entity, handling the immutable nature of record types
    /// </summary>
    /// <param name="entity">The updated entity</param>
    /// <returns>The updated entity from the database</returns>
    public override async Task UpdateAsync(Sequence entity)
    {
        try
        {
            // Fetch the existing entity
            var existingEntity = await DbSet.FindAsync(entity.Id);
            if (existingEntity == null)
                throw new EntityNotFoundException(typeof(Sequence).Name, entity.Id);
            
            // Detach existing entity to avoid tracking conflicts
            DbContext.Entry(existingEntity).State = EntityState.Detached;
            
            // Update the entity and mark it as modified
            DbSet.Update(entity);
        }
        catch (EntityNotFoundException)
        {
            // Re-throw entity not found exceptions
            throw;
        }
        catch (Exception ex)
        {
            throw new StorageProviderException($"Update-{typeof(Sequence).Name}", ex);
        }
    }
    
    /// <summary>
    /// Updates a sequence with specific property changes
    /// </summary>
    /// <param name="id">Sequence ID</param>
    /// <param name="name">Optional new name</param>
    /// <param name="worstCaseTime">Optional new worst case time</param>
    /// <param name="description">Optional new description</param>
    /// <param name="canBeParallel">Optional new canBeParallel value</param>
    /// <returns>The updated sequence</returns>
    public async Task<Sequence> UpdateSequencePropertiesAsync(
        string id, 
        string? name = null, 
        TimeSpan? worstCaseTime = null, 
        string? description = null,
        bool? canBeParallel = null)
    {
        var entity = await DbSet.FindAsync(id);
        if (entity == null)
            throw new EntityNotFoundException(typeof(Sequence).Name, id);
            
        // Use the entity's Update method to create a new instance with updated properties
        var updatedEntity = entity.Update(name, worstCaseTime, description, canBeParallel);
        
        // Detach the original entity and update with the new one
        DbContext.Entry(entity).State = EntityState.Detached;
        DbSet.Update(updatedEntity);
        await DbContext.SaveChangesAsync();
        
        return updatedEntity;
    }
}
