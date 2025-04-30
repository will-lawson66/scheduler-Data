using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Repository;
using Instrument.Scheduling.Data.Services;

namespace Instrument.Scheduling.Data;
public class UnitOfWork : IUnitOfWork
{
    private readonly DataContext.SchedulerDbContext _dbContext;
    
    private ISequenceRepository? _sequenceDefinitionRepository;
    private IParameterRepository? _parameterRepository;
    private IRangeRepository? _rangeRepository;
    private IRangeValueRepository? _rangeValueRepository;
    private IResourceRepository? _resourceRepository;
    private ISequenceGroupRepository? _sequenceGroupRepository;

    public UnitOfWork(DataContext.SchedulerDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public ISequenceRepository SequenceDefinitions =>
        _sequenceDefinitionRepository ??= new SequenceRepository(_dbContext);
        
    public IParameterRepository Parameters =>
        _parameterRepository ??= new ParameterRepository(_dbContext);
        
    public IRangeRepository Ranges =>
        _rangeRepository ??= new RangeRepository(_dbContext);
        
    public IRangeValueRepository RangeValues =>
        _rangeValueRepository ??= new RangeValueRepository(_dbContext);
        
    public IResourceRepository Resources =>
        _resourceRepository ??= new ResourceRepository(_dbContext);
        
    public ISequenceGroupRepository SequenceGroups =>
        _sequenceGroupRepository ??= new SequenceGroupRepository(_dbContext);

    public async Task<int> SaveChangesAsync()
    {
        return await _dbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
