using System.Linq;
using System.Threading.Tasks;
using Instrument.Scheduling.Data.Entities;

namespace Instrument.Scheduling.Data.Interfaces
{
    public interface ISchedulerDataContext
    {
        IQueryable<SequenceDefinition> SequenceDefinitions { get; }
        Task SaveChangesAsync();
    }
}
