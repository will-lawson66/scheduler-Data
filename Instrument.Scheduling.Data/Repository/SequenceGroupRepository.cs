using Microsoft.EntityFrameworkCore;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;

namespace Instrument.Scheduling.Data.Repository;
public class SequenceGroupRepository : ISequenceGroupRepository
{
    private readonly IStorageProvider<SequenceGroup> _storageProvider;
    private readonly ISequenceRepository _sequenceRepository;
    private readonly DataContext.SchedulerDbContext _dbContext;

    public SequenceGroupRepository(
        IStorageProvider<SequenceGroup> storageProvider,
        ISequenceRepository sequenceRepository,
        DataContext.SchedulerDbContext dbContext)
    {
        _storageProvider = storageProvider;
        _sequenceRepository = sequenceRepository;
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<SequenceGroup>> GetAllAsync()
    {
        return await _storageProvider.GetAllAsync();
    }

    public async Task<SequenceGroup?> GetByIdAsync(string id)
    {
        return await _storageProvider.GetByIdAsync(id);
    }

    public async Task<IQueryable<SequenceGroup>> GetQueryableAsync()
    {
        var data = await _storageProvider.GetAllAsync();
        return data.AsQueryable();
    }

    public async Task AddAsync(SequenceGroup sequenceGroup)
    {
        await _storageProvider.AddAsync(sequenceGroup);
    }

    public async Task UpdateAsync(SequenceGroup sequenceGroup)
    {
        await _storageProvider.UpdateAsync(sequenceGroup);
    }

    public async Task DeleteAsync(string id)
    {
        await _storageProvider.DeleteAsync(id);
    }

    public async Task SaveChangesAsync()
    {
        await _storageProvider.SaveChangesAsync();
    }

    // Specialized methods for SequenceGroup
    public async Task<SequenceGroup?> GetWithSequencesAsync(string id)
    {
        return await _dbContext.SequenceGroups
            .Include(sg => sg.SequenceGroupSequences)
            .ThenInclude(sgs => sgs.Sequence)
            .FirstOrDefaultAsync(sg => sg.Id == id);
    }

    public async Task<SortedList<int, Sequence>> GetOrderedSequencesAsync(string sequenceGroupId)
    {
        // Get all SequenceGroupSequences for the specified SequenceGroup
        var sequenceGroupSequences = await _dbContext.SequenceGroupSequences
            .Where(sgs => sgs.SequenceGroupId == sequenceGroupId)
            .Include(sgs => sgs.Sequence)
            .ToListAsync();

        // Create a SortedList with the Order as key and Sequence as value
        var sortedSequences = new SortedList<int, Sequence>();
        
        foreach (var sgs in sequenceGroupSequences)
        {
            if (sgs.Sequence != null)
            {
                sortedSequences.Add(sgs.Order, sgs.Sequence);
            }
        }
        
        return sortedSequences;
    }
}
