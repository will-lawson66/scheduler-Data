namespace Instrument.Scheduling.Data.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        ISequenceDefinitionRepository SequenceDefinitions { get; }
        Task<int> SaveChangesAsync();
    }
}
