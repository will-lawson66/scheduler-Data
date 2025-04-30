using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Scheduling.Data.Repository;

/// <summary>
/// Base repository implementation that works with the DbContext
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly SchedulerDbContext DbContext;
    protected readonly DbSet<T> DbSet;

    public Repository(SchedulerDbContext dbContext)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        DbSet = dbContext.Set<T>();
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        try
        {
            return await DbSet.ToListAsync();
        }
        catch (Exception ex)
        {
            throw new StorageProviderException($"GetAll-{typeof(T).Name}", ex);
        }
    }

    public virtual async Task<T?> GetByIdAsync(string id)
    {
        try
        {
            return await DbSet.FindAsync(id);
        }
        catch (Exception ex)
        {
            throw new StorageProviderException($"GetById-{typeof(T).Name}", ex);
        }
    }

    public virtual Task<IQueryable<T>> GetQueryableAsync()
    {
        try
        {
            return Task.FromResult(DbSet.AsQueryable());
        }
        catch (Exception ex)
        {
            throw new StorageProviderException($"GetQueryable-{typeof(T).Name}", ex);
        }
    }

    public virtual async Task AddAsync(T entity)
    {
        try
        {
            await DbSet.AddAsync(entity);
        }
        catch (Exception ex)
        {
            throw new StorageProviderException($"Add-{typeof(T).Name}", ex);
        }
    }

    public virtual Task UpdateAsync(T entity)
    {
        try
        {
            DbSet.Update(entity);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new StorageProviderException($"Update-{typeof(T).Name}", ex);
        }
    }

    public virtual async Task DeleteAsync(string id)
    {
        try
        {
            var entity = await DbSet.FindAsync(id);
            if (entity == null)
            {
                throw new EntityNotFoundException(typeof(T).Name, id);
            }
            
            DbSet.Remove(entity);
        }
        catch (EntityNotFoundException)
        {
            // Re-throw entity not found exceptions
            throw;
        }
        catch (Exception ex)
        {
            throw new StorageProviderException($"Delete-{typeof(T).Name}", ex);
        }
    }

    public virtual async Task SaveChangesAsync()
    {
        try
        {
            await DbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new StorageProviderException($"SaveChanges_Concurrency-{typeof(T).Name}", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new StorageProviderException($"SaveChanges_Update-{typeof(T).Name}", ex);
        }
        catch (Exception ex)
        {
            throw new StorageProviderException($"SaveChanges-{typeof(T).Name}", ex);
        }
    }
}
