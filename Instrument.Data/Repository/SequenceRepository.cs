using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Data.Repository;

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

    /// <inheritdoc />
    public async Task<Sequence?> GetSequenceWithParametersAsync(int id)
    {
        return await DbContext.Sequences
            .Include(s => s.SequenceParameters)
            .ThenInclude(sp => sp.Parameter)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Sequence>> GetSequencesByPartialNameAsync(string name)
    {
        return await DbContext.Sequences
            .Where(s => s.Name.Contains(name))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task RemoveParameterFromSequenceAsync(int parameterId, int sequenceId)
    {
        var sequenceParameter = await DbContext.SequenceParameters
            .FirstOrDefaultAsync(sp => sp.ParameterId == parameterId && sp.SequenceId == sequenceId);
            
        if (sequenceParameter != null)
        {
            DbContext.SequenceParameters.Remove(sequenceParameter);
            await DbContext.SaveChangesAsync();
        }
    }
}
