using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Repository;

namespace Instrument.Scheduling.Data;
public class UnitOfWork : IUnitOfWork
{
    private readonly IStorageProvider<Sequence> _sequenceStorageProvider;
    private readonly IStorageProvider<Parameter> _parameterStorageProvider;
    private readonly IStorageProvider<SequenceParameter> _sequenceParameterStorageProvider;
    private readonly IStorageProvider<Entities.Range> _rangeStorageProvider;
    private readonly IStorageProvider<RangeValue> _rangeValueStorageProvider;
    private readonly IStorageProvider<Resource> _resourceStorageProvider;
    
    private ISequenceRepository? _sequenceDefinitionRepository;
    private IParameterRepository? _parameterRepository;
    private IRangeRepository? _rangeRepository;
    private IRangeValueRepository? _rangeValueRepository;
    private IResourceRepository? _resourceRepository;

    public UnitOfWork(
        IStorageProvider<Sequence> sequenceStorageProvider,
        IStorageProvider<Parameter> parameterStorageProvider,
        IStorageProvider<SequenceParameter> sequenceParameterStorageProvider,
        IStorageProvider<Entities.Range> rangeStorageProvider,
        IStorageProvider<RangeValue> rangeValueStorageProvider,
        IStorageProvider<Resource> resourceStorageProvider)
    {
        _sequenceStorageProvider = sequenceStorageProvider;
        _parameterStorageProvider = parameterStorageProvider;
        _sequenceParameterStorageProvider = sequenceParameterStorageProvider;
        _rangeStorageProvider = rangeStorageProvider;
        _rangeValueStorageProvider = rangeValueStorageProvider;
        _resourceStorageProvider = resourceStorageProvider;
    }

    public ISequenceRepository SequenceDefinitions =>
        _sequenceDefinitionRepository ??= new SequenceRepository(_sequenceStorageProvider);
        
    public IParameterRepository Parameters =>
        _parameterRepository ??= new ParameterRepository(_parameterStorageProvider, _sequenceParameterStorageProvider);
        
    public IRangeRepository Ranges =>
        _rangeRepository ??= new RangeRepository(_rangeStorageProvider, _parameterStorageProvider);
        
    public IRangeValueRepository RangeValues =>
        _rangeValueRepository ??= new RangeValueRepository(_rangeValueStorageProvider);
        
    public IResourceRepository Resources =>
        _resourceRepository ??= new ResourceRepository(_resourceStorageProvider, _parameterStorageProvider);

    public async Task<int> SaveChangesAsync()
    {
        await _sequenceStorageProvider.SaveChangesAsync();
        await _parameterStorageProvider.SaveChangesAsync();
        await _sequenceParameterStorageProvider.SaveChangesAsync();
        await _rangeStorageProvider.SaveChangesAsync();
        await _rangeValueStorageProvider.SaveChangesAsync();
        await _resourceStorageProvider.SaveChangesAsync();
        // In a real implementation, you'd track changes and return the count
        return 1;
    }

    public void Dispose()
    {
        // Cleanup resources if needed
    }
}
