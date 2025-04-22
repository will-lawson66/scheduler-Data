using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Repository;

namespace Instrument.Scheduling.Data;
public class UnitOfWork : IUnitOfWork
{
    private readonly IStorageProvider<Sequence> _sequenceStorageProvider;
    private ISequenceRepository? _sequenceDefinitionRepository;

    public UnitOfWork(IStorageProvider<Sequence> sequenceStorageProvider)
    {
        _sequenceStorageProvider = sequenceStorageProvider;
    }

    public ISequenceRepository SequenceDefinitions =>
        _sequenceDefinitionRepository ??= new SequenceRepository(_sequenceStorageProvider);

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
