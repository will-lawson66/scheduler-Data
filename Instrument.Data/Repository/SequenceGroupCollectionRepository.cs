using Instrument.Data.Entities;
using Instrument.Data.DataContext;
using Microsoft.EntityFrameworkCore;
using Instrument.Data.Exceptions;

namespace Instrument.Data.Repository;
public class SequenceGroupCollectionRepository<TEnum>
    : Repository<SequenceGroupCollection<TEnum>>, Instrument.Data.ISequenceGroupCollectionRepository<TEnum>
        where TEnum : Enum
{
    public SequenceGroupCollectionRepository(SchedulerDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<SequenceGroupCollection<TEnum>>> GetSequenceGroupCollectionsByCategoryAsync(TEnum category)
    {
        return await DbContext.Set<SequenceGroupCollection<TEnum>>()
            .Where(sgc => sgc.Category != null && sgc.Category.Equals(category))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task AddSequenceGroupToSequenceGroupCollectionAsync(int sequenceGroupCollectionId, int sequenceGroupId, int order)
    {
        // retrieve the SequenceGroupCollection with its SequenceGroupCollectionSequenceGroup data
        var sequenceGroupCollection
            = await GetByIdAsync(sequenceGroupCollectionId)
              ?? throw new EntityNotFoundException("SequenceGroupCollection", sequenceGroupCollectionId);

        // if order is default, find the highest order number in SequenceGroupCollectionSequenceGroups and increment
        if (order == 0) // todo: factor out and test this function
        {
            var sequenceGroupCollectionSequenceGroups
                = sequenceGroupCollection.SequenceGroupCollectionSequenceGroups;
                
            var maxOrderGroup = sequenceGroupCollectionSequenceGroups
                .OrderByDescending(s => s.Order)
                .FirstOrDefault();
            var newOrder = maxOrderGroup == null ? 1 : maxOrderGroup.Order + 1;

            await DbContext.SequenceGroupCollectionSequenceGroups.AddAsync(new SequenceGroupCollectionSequenceGroup
            {
                SequenceGroupCollectionId = sequenceGroupCollectionId,
                SequenceGroupId = sequenceGroupId,
                Order = newOrder
            });
        }
        else
        {
            // Check if there is already a sequence group at the specified order
            var existingSequenceGroupCollectionSequenceGroup = sequenceGroupCollection.SequenceGroupCollectionSequenceGroups
                .FirstOrDefault(sgs => sgs.SequenceGroupId == sequenceGroupId && sgs.Order == order);

            if (existingSequenceGroupCollectionSequenceGroup != null)
            {
                // If there is already a sequence at this order, we need to shift all higher orders up
                var sequenceGroupsToShift = await DbContext.SequenceGroupCollectionSequenceGroups
                    .Where(sgs => sgs.SequenceGroupCollectionId == sequenceGroupCollectionId && sgs.Order >= order)
                    .OrderByDescending(sgs => sgs.Order)
                    .ToListAsync();

                foreach (var seqToShift in sequenceGroupsToShift)
                {
                    // Create a new entity with the updated order
                    var updatedSequence = new SequenceGroupCollectionSequenceGroup
                    {
                        SequenceGroupCollectionId = seqToShift.SequenceGroupCollectionId,
                        SequenceGroupId = seqToShift.SequenceGroupId,
                        Order = seqToShift.Order + 1
                    };

                    // Remove the old entity and add the new one
                    DbContext.SequenceGroupCollectionSequenceGroups.Remove(seqToShift);
                    await DbContext.SequenceGroupCollectionSequenceGroups.AddAsync(updatedSequence);
                }
            }
            else
            {
                await DbContext.SequenceGroupCollectionSequenceGroups.AddAsync(new SequenceGroupCollectionSequenceGroup
                {
                    SequenceGroupCollectionId = sequenceGroupCollectionId,
                    SequenceGroupId = sequenceGroupId,
                    Order = order
                });
            }
        }

        await DbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<SortedList<int, SequenceGroup>> GetOrderedSequenceGroupsAsync(int collectionId)
    {
        // Get all SequenceGroupSequences for the specified SequenceGroup
        var sequenceGroupCollectionSequenceGroups = await DbContext.SequenceGroupCollectionSequenceGroups
            .Where(sgs => sgs.SequenceGroupCollectionId == collectionId)
            .Include(sgs => sgs.SequenceGroup)
            .ToListAsync();

        // Create a SortedList with the Order as key and Sequence as value
        var sortedSequenceGroups = new SortedList<int, SequenceGroup>();

        foreach (var sgs in sequenceGroupCollectionSequenceGroups)
        {
            if (sgs.SequenceGroup != null)
            {
                sortedSequenceGroups.Add(sgs.Order, sgs.SequenceGroup);
            }
        }

        return sortedSequenceGroups;
    }
}
