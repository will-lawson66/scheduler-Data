namespace Instrument.Scheduling.Data.Interfaces;
public interface IUnitOfWork : IDisposable
{
    ISequenceRepository SequenceDefinitions { get; }
    Task<int> SaveChangesAsync();
}
