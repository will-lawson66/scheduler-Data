using System;
using System.Threading.Tasks;

namespace Scheduler.DataLayer.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        ISequenceDefinitionRepository SequenceDefinitions { get; }
        Task<int> SaveChangesAsync();
    }
}
