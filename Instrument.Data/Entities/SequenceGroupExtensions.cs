using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;

namespace Instrument\.Data.Entities;

/// <summary>
/// Extension methods for working with SequenceGroups and their sequences
/// </summary>
public static class SequenceGroupExtensions
{
    /// <summary>
    /// Gets a sorted list of sequences associated with a sequence group
    /// </summary>
    /// <param name="sequenceGroup">The sequence group to get sequences for</param>
    /// <param name="dbContext">Database context</param>
    /// <returns>A sorted list of sequences in their defined order</returns>
    public static async Task<SortedList<int, Sequence>> GetOrderedSequencesAsync(
        this SequenceGroup sequenceGroup,
        DataContext.SchedulerDbContext dbContext)
    {
        if (sequenceGroup == null)
            throw new ArgumentNullException(nameof(sequenceGroup));
            
        if (dbContext == null)
            throw new ArgumentNullException(nameof(dbContext));

        // Get all SequenceGroupSequences for the specified SequenceGroup
        var sequenceGroupSequences = await dbContext.SequenceGroupSequences
            .Where(sgs => sgs.SequenceGroupId == sequenceGroup.Id)
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

    /// <summary>
    /// Gets an immutable list of sequences associated with a sequence group
    /// </summary>
    /// <param name="sequenceGroup">The sequence group to get sequences for</param>
    /// <param name="dbContext">Database context</param>
    /// <returns>An immutable list of sequences in their defined order</returns>
    public static async Task<ImmutableList<Sequence>> GetSequencesAsImmutableListAsync(
        this SequenceGroup sequenceGroup,
        DataContext.SchedulerDbContext dbContext)
    {
        var orderedSequences = await GetOrderedSequencesAsync(sequenceGroup, dbContext);
        return ImmutableList.CreateRange(orderedSequences.Values);
    }
    
    /// <summary>
    /// Adds a sequence to a sequence group in the specified order
    /// </summary>
    /// <param name="sequenceGroup">The sequence group to add the sequence to</param>
    /// <param name="sequence">The sequence to add</param>
    /// <param name="order">The order of the sequence within the group</param>
    /// <param name="dbContext">Database context</param>
    public static async Task AddSequenceAsync(
        this SequenceGroup sequenceGroup,
        Sequence sequence,
        int order,
        DataContext.SchedulerDbContext dbContext)
    {
        if (sequenceGroup == null)
            throw new ArgumentNullException(nameof(sequenceGroup));
            
        if (sequence == null)
            throw new ArgumentNullException(nameof(sequence));
            
        if (dbContext == null)
            throw new ArgumentNullException(nameof(dbContext));

        // Check if there is already a sequence at the specified order
        var existingSequence = await dbContext.SequenceGroupSequences
            .FirstOrDefaultAsync(sgs => sgs.SequenceGroupId == sequenceGroup.Id && sgs.Order == order);

        if (existingSequence != null)
        {
            // If there is already a sequence at this order, we need to shift all higher orders up
            var sequencesToShift = await dbContext.SequenceGroupSequences
                .Where(sgs => sgs.SequenceGroupId == sequenceGroup.Id && sgs.Order >= order)
                .OrderByDescending(sgs => sgs.Order)
                .ToListAsync();

            foreach (var seqToShift in sequencesToShift)
            {
                // Create a new entity with the updated order
                var updatedSequence = new SequenceGroupSequences
                {
                    SequenceGroupId = seqToShift.SequenceGroupId,
                    SequenceId = seqToShift.SequenceId,
                    Order = seqToShift.Order + 1
                };

                // Remove the old entity and add the new one
                dbContext.SequenceGroupSequences.Remove(seqToShift);
                await dbContext.SequenceGroupSequences.AddAsync(updatedSequence);
            }
        }

        // Add the new sequence at the specified order
        await dbContext.SequenceGroupSequences.AddAsync(new SequenceGroupSequences
        {
            SequenceGroupId = sequenceGroup.Id,
            SequenceId = sequence.Id,
            Order = order
        });

        await dbContext.SaveChangesAsync();
    }
}
