using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Instrument.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Data.Repository;

/// <summary>
/// Repository for parameters
/// </summary>
public class ParameterRepository : Repository<Parameter>, IParameterRepository
{
    /// <summary>
    /// Creates a new parameter repository
    /// </summary>
    /// <param name="dbContext">Database context</param>
    public ParameterRepository(SchedulerDbContext dbContext)
        : base(dbContext)
    {
    }
    
    /// <summary>
    /// Gets parameters for a sequence
    /// </summary>
    /// <param name="sequenceId">Sequence ID</param>
    public async Task<IEnumerable<Parameter>> GetParametersForSequenceAsync(string sequenceId)
    {
        return await DbContext.SequenceParameters
            .Where(sp => sp.SequenceId == sequenceId)
            .Include(sp => sp.Parameter)
            .Select(sp => sp.Parameter)
            .ToListAsync();
    }

    /// <summary>
    /// Adds a parameter to a sequence
    /// </summary>
    /// <param name="parameterId">Parameter ID</param>
    /// <param name="sequenceId">Sequence ID</param>
    /// <param name="orderNumber">Order number</param>
    public async Task AddParameterToSequenceAsync(string parameterId, string sequenceId, int orderNumber)
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
}