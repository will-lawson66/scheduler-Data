namespace Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Services;

public interface IUnitOfWork : IDisposable
{
    ISequenceRepository SequenceDefinitions { get; }
    IParameterRepository Parameters { get; }
    IRangeRepository Ranges { get; }
    IRangeValueRepository RangeValues { get; }
    IResourceRepository Resources { get; }
    ISequenceGroupRepository SequenceGroups { get; }
    SequenceGroupService SequenceGroupService { get; }
    Task<int> SaveChangesAsync();
}
