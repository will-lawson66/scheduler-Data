using Instrument\.Data.DataContext;
using Instrument\.Data.Entities;
using Instrument\.Data.Exceptions;
using Instrument\.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Instrument\.Data.Repository;

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
}
