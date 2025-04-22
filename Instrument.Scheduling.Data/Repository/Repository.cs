using Instrument.Scheduling.Data.Interfaces;

namespace Instrument.Scheduling.Data.Domain.Repository;
public class Repository<T>(IDataContext context) : IRepository<T>
    where T : class
{
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return context.Data<T>().ToList();
    }

    public Task<T> GetByIdAsync(int id)
    {
        throw new NotImplementedException();
    }
}
