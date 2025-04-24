namespace Instrument.Scheduling.Data.Interfaces;
public interface IUnitOfWork : IDisposable
{
    ISequenceRepository SequenceDefinitions { get; }
    IParameterRepository Parameters { get; }
    IRangeRepository Ranges { get; }
    IRangeValueRepository RangeValues { get; }
    IResourceRepository Resources { get; }
    Task<int> SaveChangesAsync();
}
