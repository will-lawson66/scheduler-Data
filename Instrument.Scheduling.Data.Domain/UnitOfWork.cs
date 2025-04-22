using System.Threading.Tasks;
using Scheduler.DataLayer.Entities;
using Scheduler.DataLayer.Interfaces;
using Scheduler.DataLayer.Repositories;

namespace Scheduler.DataLayer.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IStorageProvider<SequenceDefinition> _sequenceStorageProvider;
        private ISequenceDefinitionRepository? _sequenceDefinitionRepository;

        public UnitOfWork(IStorageProvider<SequenceDefinition> sequenceStorageProvider)
        {
            _sequenceStorageProvider = sequenceStorageProvider;
        }

        public ISequenceDefinitionRepository SequenceDefinitions =>
            _sequenceDefinitionRepository ??= new SequenceDefinitionRepository(_sequenceStorageProvider);

        public async Task<int> SaveChangesAsync()
        {
            await _sequenceStorageProvider.SaveChangesAsync();
            // In a real implementation, you'd track changes and return the count
            return 1;
        }

        public void Dispose()
        {
            // Cleanup resources if needed
        }
    }
}
