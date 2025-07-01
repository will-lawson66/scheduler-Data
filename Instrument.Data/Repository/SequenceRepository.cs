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
    public async Task<Sequence?> GetSequenceWithParametersAsync(int sequenceId)
    {
        return await DbContext.Sequences
            .Include(s => s.SequenceParameters)
            .ThenInclude(sp => sp.Parameter)
            .FirstOrDefaultAsync(s => s.Id == sequenceId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Sequence>> GetSequencesByPartialNameAsync(string name)
    {
        return await DbContext.Sequences
            .Where(s => s.Name.Contains(name))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task AddParameterToSequenceAsync(int parameterId, int sequenceId, int orderNumber = 0)
    {
        // Check if the parameter and sequence exist
        var parameter = await DbContext.Parameters.FindAsync(parameterId);
        var sequence = await DbContext.Sequences.FindAsync(sequenceId);

        if (parameter == null || sequence == null)
        {
            throw new Exceptions.EntityNotFoundException(parameter == null ? "Parameter" : "Sequence",
                parameter == null ? parameterId : sequenceId);
        }

        // Check if the association already exists
        var existingAssociation = await DbContext.SequenceParameters
            .FirstOrDefaultAsync(sp => sp.ParameterId == parameterId && sp.SequenceId == sequenceId);

        if (existingAssociation != null)
        {
            // Todo: throw an exception?

            // Update the order number if it's different
            if (existingAssociation.OrderNumber != orderNumber)
            {
                existingAssociation.OrderNumber = orderNumber;
                DbContext.SequenceParameters.Update(existingAssociation);
            }
        }
        else
        {
            // Create a new association
            await DbContext.SequenceParameters.AddAsync(new SequenceParameter
            {
                ParameterId = parameterId,
                SequenceId = sequenceId,
                OrderNumber = orderNumber
            });
        }

        await DbContext.SaveChangesAsync();
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

    /// <inheritdoc />
    public async Task<IEnumerable<Sequence>> GetSequencesWithParametersAsync(string? name = null)
    {
        var query = DbContext.Sequences
            .Include(s => s.SequenceParameters)
                .ThenInclude(sp => sp.Parameter)
            .AsQueryable();

        // Apply name filter if provided
        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(s => s.Name == name);
        }

        return await query.ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Parameter>> GetOrderedParametersAsync(int sequenceId)
    {
        var parameters = await DbContext.SequenceParameters
            .Where(sp => sp.SequenceId == sequenceId)
            .Include(sp => sp.Parameter)
            .OrderBy(sp => sp.OrderNumber)
            .Select(sp => sp.Parameter!)
            .ToListAsync();

        return parameters;
    }
}
