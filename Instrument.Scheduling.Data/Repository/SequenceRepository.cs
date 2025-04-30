using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
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
}