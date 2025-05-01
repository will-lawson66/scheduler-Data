using Microsoft.EntityFrameworkCore;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.DataContext;

namespace Instrument.Scheduling.Data.Repository;
public class SequenceGroupRepository : Repository<SequenceGroup>, ISequenceGroupRepository
{
    public SequenceGroupRepository(SchedulerDbContext dbContext) : base(dbContext)
    {
    }

    // Specialized methods for SequenceGroup
    public async Task<SequenceGroup?> GetWithSequencesAsync(string id)
    {
        return await DbContext.SequenceGroups
            .Include(sg => sg.SequenceGroupSequences)
            .ThenInclude(sgs => sgs.Sequence)
            .FirstOrDefaultAsync(sg => sg.Id == id);
    }

    public async Task<SortedList<int, Sequence>> GetOrderedSequencesAsync(string sequenceGroupId)
    {
        // Get all SequenceGroupSequences for the specified SequenceGroup
        var sequenceGroupSequences = await DbContext.SequenceGroupSequences
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
