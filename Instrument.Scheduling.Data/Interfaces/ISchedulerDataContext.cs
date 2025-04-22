using Instrument.Scheduling.Data.Entities;

namespace Instrument.Scheduling.Data.Interfaces;
public interface ISchedulerDataContext
{
    IQueryable<Sequence> Sequences { get; }
    Task SaveChangesAsync();
}
