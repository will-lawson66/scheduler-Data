using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Interfaces;
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
        try
        {
            return await _context.Set<T>().ToListAsync();
        }
        catch (Exception ex)
        {
            throw new StorageProviderException("GetAll", ex);
        }
    }

    public async Task<T?> GetByIdAsync(string id)
    {
        try
        {
            var entity = await _context.Set<T>().FindAsync(id);
            return entity;
        }
        catch (Exception ex)
        {
            throw new StorageProviderException("GetById", ex);
        }
    }

    public async Task AddAsync(T entity)
    {
        try
        {
            await _context.Set<T>().AddAsync(entity);
        }
        catch (Exception ex)
        {
            throw new StorageProviderException("Add", ex);
        }
    }

    public Task UpdateAsync(T entity)
    {
        try
        {
            _context.Set<T>().Update(entity);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new StorageProviderException("Update", ex);
        }
    }

    public Task DeleteAsync(string id)
    {
        try
        {
            var entity = _context.Set<T>().Find(id);
            if (entity != null)
            {
                _context.Set<T>().Remove(entity);
            }
            else
            {
                throw new EntityNotFoundException(typeof(T).Name, id);
            }
            return Task.CompletedTask;
        }
        catch (EntityNotFoundException)
        {
            // Re-throw entity not found exceptions
            throw;
        }
        catch (Exception ex)
        {
            throw new StorageProviderException("Delete", ex);
        }
    }

    public async Task SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new StorageProviderException("SaveChanges_Concurrency", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new StorageProviderException("SaveChanges_Update", ex);
        }
        catch (Exception ex)
        {
            throw new StorageProviderException("SaveChanges", ex);
        }
    }
}
