namespace Instrument.Scheduling.Data.Interfaces;
public interface IUnitOfWork : IDisposable
{
    ISequenceRepository SequenceDefinitions { get; }
    IParameterRepository Parameters { get; }
    Task<int> SaveChangesAsync();
}
