using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Scheduling.Data.Providers;

// This provider handles the SQLite implementation with special handling for composite keys
public class SqliteSequenceParameterProvider : IStorageProvider<SequenceParameter>
{
    private readonly SchedulerDbContext _context;

    public SqliteSequenceParameterProvider(SchedulerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SequenceParameter>> GetAllAsync()
    {
        return await _context.SequenceParameters
            .Include(sp => sp.Parameter)
            .Include(sp => sp.Sequence)
            .ToListAsync();
    }

    public async Task<SequenceParameter?> GetByIdAsync(string id)
    {
        // The ID for a junction entity might be a composite key formatted as "sequenceId_parameterId"
        if (id.Contains('_'))
        {
            var parts = id.Split('_');
            if (parts.Length == 2)
            {
                string sequenceId = parts[0];
                string parameterId = parts[1];
                
                return await _context.SequenceParameters
                    .Include(sp => sp.Parameter)
                    .Include(sp => sp.Sequence)
                    .FirstOrDefaultAsync(sp => 
                        sp.SequenceId == sequenceId && 
                        sp.ParameterId == parameterId);
            }
        }
        
        return null;
    }

    public async Task AddAsync(SequenceParameter entity)
    {
        // Check if it already exists
        var existing = await _context.SequenceParameters
            .FirstOrDefaultAsync(sp => 
                sp.SequenceId == entity.SequenceId && 
                sp.ParameterId == entity.ParameterId);
                
        if (existing == null)
        {
            await _context.SequenceParameters.AddAsync(entity);
        }
    }

    public Task UpdateAsync(SequenceParameter entity)
    {
        // Find and update the entity (no tracking issues in EF Core)
        _context.SequenceParameters.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _context.SequenceParameters.Remove(entity);
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
