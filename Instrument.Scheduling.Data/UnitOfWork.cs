using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Repository;
using Instrument.Scheduling.Data.Services;

namespace Instrument.Scheduling.Data;
public class UnitOfWork : IUnitOfWork
{
    private readonly IStorageProvider<Sequence> _sequenceStorageProvider;
    private readonly IStorageProvider<Parameter> _parameterStorageProvider;
    private readonly IStorageProvider<SequenceParameter> _sequenceParameterStorageProvider;
    private readonly IStorageProvider<Entities.Range> _rangeStorageProvider;
    private readonly IStorageProvider<RangeValue> _rangeValueStorageProvider;
    private readonly IStorageProvider<Resource> _resourceStorageProvider;
    private readonly IStorageProvider<SequenceGroup> _sequenceGroupStorageProvider;
    private readonly DataContext.SchedulerDbContext _dbContext;
    
    private ISequenceRepository? _sequenceDefinitionRepository;
    private IParameterRepository? _parameterRepository;
    private IRangeRepository? _rangeRepository;
    private IRangeValueRepository? _rangeValueRepository;
    private IResourceRepository? _resourceRepository;
    private ISequenceGroupRepository? _sequenceGroupRepository;
    private SequenceGroupService? _sequenceGroupService;

    public UnitOfWork(
        IStorageProvider<Sequence> sequenceStorageProvider,
        IStorageProvider<Parameter> parameterStorageProvider,
        IStorageProvider<SequenceParameter> sequenceParameterStorageProvider,
        IStorageProvider<Entities.Range> rangeStorageProvider,
        IStorageProvider<RangeValue> rangeValueStorageProvider,
        IStorageProvider<Resource> resourceStorageProvider,
        IStorageProvider<SequenceGroup> sequenceGroupStorageProvider,
        DataContext.SchedulerDbContext dbContext,
        SequenceGroupService sequenceGroupService)
    {
        _sequenceStorageProvider = sequenceStorageProvider ?? throw new ArgumentNullException(nameof(sequenceStorageProvider));
        _parameterStorageProvider = parameterStorageProvider ?? throw new ArgumentNullException(nameof(parameterStorageProvider));
        _sequenceParameterStorageProvider = sequenceParameterStorageProvider ?? throw new ArgumentNullException(nameof(sequenceParameterStorageProvider));
        _rangeStorageProvider = rangeStorageProvider ?? throw new ArgumentNullException(nameof(rangeStorageProvider));
        _rangeValueStorageProvider = rangeValueStorageProvider ?? throw new ArgumentNullException(nameof(rangeValueStorageProvider));
        _resourceStorageProvider = resourceStorageProvider ?? throw new ArgumentNullException(nameof(resourceStorageProvider));
        _sequenceGroupStorageProvider = sequenceGroupStorageProvider ?? throw new ArgumentNullException(nameof(sequenceGroupStorageProvider));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _sequenceGroupService = sequenceGroupService ?? throw new ArgumentNullException(nameof(sequenceGroupService));
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
        
    public ISequenceGroupRepository SequenceGroups =>
        _sequenceGroupRepository ??= new SequenceGroupRepository(_sequenceGroupStorageProvider, SequenceDefinitions, _dbContext);
        
    public SequenceGroupService SequenceGroupService =>
        _sequenceGroupService;

    public async Task<int> SaveChangesAsync()
    {
        await _sequenceStorageProvider.SaveChangesAsync();
        await _parameterStorageProvider.SaveChangesAsync();
        await _sequenceParameterStorageProvider.SaveChangesAsync();
        await _rangeStorageProvider.SaveChangesAsync();
        await _rangeValueStorageProvider.SaveChangesAsync();
        await _resourceStorageProvider.SaveChangesAsync();
        await _sequenceGroupStorageProvider.SaveChangesAsync();
        return 1;
    }

    public void Dispose()
    {
        // Cleanup resources if needed
    }
}
