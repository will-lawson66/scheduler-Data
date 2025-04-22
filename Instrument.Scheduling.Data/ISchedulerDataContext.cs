using System.Linq;
using System.Threading.Tasks;
using Scheduler.DataLayer.Entities;

namespace Scheduler.DataLayer.Interfaces
{
    public interface ISchedulerDataContext
    {
        IQueryable<SequenceDefinition> SequenceDefinitions { get; }
        Task SaveChangesAsync();
    }
}
