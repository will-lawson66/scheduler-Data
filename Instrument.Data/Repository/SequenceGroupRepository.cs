using Microsoft.EntityFrameworkCore;
using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;
using Instrument.Data.DataContext;
using Instrument.Data.Exceptions;

namespace Instrument.Data.Repository;
public class SequenceGroupRepository : Repository<SequenceGroup>, ISequenceGroupRepository
{
    /// <summary>
    /// Repository for SequenceGroups
    /// </summary>
    /// <param name="dbContext"></param>
    public SequenceGroupRepository(SchedulerDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <inheritdoc />
    public async Task AddSequenceToSequenceGroupAsync(int sequenceGroupId, int sequenceId, int order = 0)
    {
        // retrieve the SequenceGroup with its SequenceGroupSequence data
        var sequenceGroup
            = await GetByIdAsync(sequenceGroupId)
              ?? throw new EntityNotFoundException("SequenceGroup", sequenceGroupId);

        // if order is default, find the highest order number in SequenceGroupSequences and increment
        if (order == 0)
        {
            var sequenceGroupSequences = sequenceGroup.SequenceGroupSequences;
            var maxOrderSequence = sequenceGroupSequences
                .OrderByDescending(s => s.Order)
                .FirstOrDefault();
            var newOrder = maxOrderSequence == null ? 1 : maxOrderSequence.Order + 1;

            await DbContext.SequenceGroupSequences.AddAsync(new SequenceGroupSequence
            {
                SequenceGroupId = sequenceGroupId,
                SequenceId = sequenceId,
                Order = newOrder
            });
        }
        else
        {
            // Check if there is already a sequence at the specified order
            var existingSequenceGroupSequence = sequenceGroup.SequenceGroupSequences
                .FirstOrDefault(sgs => sgs.SequenceId == sequenceId && sgs.Order == order);

            if (existingSequenceGroupSequence != null)
            {
                // If there is already a sequence at this order, we need to shift all higher orders up
                var sequencesToShift = await DbContext.SequenceGroupSequences
                    .Where(sgs => sgs.SequenceGroupId == sequenceGroupId && sgs.Order >= order)
                    .OrderByDescending(sgs => sgs.Order)
                    .ToListAsync();

                foreach (var seqToShift in sequencesToShift)
                {
                    // Create a new entity with the updated order
                    var updatedSequence = new SequenceGroupSequence
                    {
                        SequenceGroupId = seqToShift.SequenceGroupId,
                        SequenceId = seqToShift.SequenceId,
                        Order = seqToShift.Order + 1
                    };

                    // Remove the old entity and add the new one
                    DbContext.SequenceGroupSequences.Remove(seqToShift);
                    await DbContext.SequenceGroupSequences.AddAsync(updatedSequence);
                }
            }
            else
            {
                await DbContext.SequenceGroupSequences.AddAsync(new SequenceGroupSequence
                {
                    SequenceGroupId = sequenceGroupId,
                    SequenceId = sequenceId,
                    Order = order
                });
            }
        }

        await DbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<SortedList<int, Sequence>> GetOrderedSequencesAsync(int sequenceGroupId)
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

    /// <inheritdoc />
    public async Task<IEnumerable<SequenceGroup>> GetSequenceGroupsWithSequencesAsync(string? name = null, Technology? technology = null)
    {
        var query = DbContext.SequenceGroups
            .Include(sg => sg.SequenceGroupSequences)
                .ThenInclude(sgs => sgs.Sequence)
            .AsQueryable();

        // Apply name filter if provided
        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(sg => sg.Name == name);
        }

        // Apply technology filter if provided
        if (technology.HasValue)
        {
            query = query.Where(sg => sg.Technology == technology);
        }

        return await query.ToListAsync();
    }
}
