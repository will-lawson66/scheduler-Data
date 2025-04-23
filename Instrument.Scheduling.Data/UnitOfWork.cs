using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Repository;

namespace Instrument.Scheduling.Data;
public class UnitOfWork : IUnitOfWork
{
    private readonly IStorageProvider<Sequence> _sequenceStorageProvider;
    private readonly IStorageProvider<Parameter> _parameterStorageProvider;
    private readonly IStorageProvider<SequenceParameter> _sequenceParameterStorageProvider;
    
    private ISequenceRepository? _sequenceDefinitionRepository;
    private IParameterRepository? _parameterRepository;

    public UnitOfWork(
        IStorageProvider<Sequence> sequenceStorageProvider,
        IStorageProvider<Parameter> parameterStorageProvider,
        IStorageProvider<SequenceParameter> sequenceParameterStorageProvider)
    {
        _sequenceStorageProvider = sequenceStorageProvider;
        _parameterStorageProvider = parameterStorageProvider;
        _sequenceParameterStorageProvider = sequenceParameterStorageProvider;
    }

    public ISequenceRepository SequenceDefinitions =>
        _sequenceDefinitionRepository ??= new SequenceRepository(_sequenceStorageProvider);
        
    public IParameterRepository Parameters =>
        _parameterRepository ??= new ParameterRepository(_parameterStorageProvider, _sequenceParameterStorageProvider);

    public async Task<int> SaveChangesAsync()
    {
        await _sequenceStorageProvider.SaveChangesAsync();
        await _parameterStorageProvider.SaveChangesAsync();
        await _sequenceParameterStorageProvider.SaveChangesAsync();
        // In a real implementation, you'd track changes and return the count
        return 1;
    }

    public void Dispose()
    {
        // Cleanup resources if needed
    }
}
