using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.DataContext;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Scheduling.Data.Providers;
public class SqliteStorageProvider<T> : IStorageProvider<T> where T : class
{
    private readonly SchedulerDbContext _context;

    public SqliteStorageProvider(SchedulerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _context.Set<T>().ToListAsync();
    }

    public async Task<T?> GetByIdAsync(string id)
    {
        return await _context.Set<T>().FindAsync(id);
    }

    public async Task AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
    }

    public Task UpdateAsync(T entity)
    {
        _context.Set<T>().Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        var entity = _context.Set<T>().Find(id);
        if (entity != null)
        {
            _context.Set<T>().Remove(entity);
        }
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}