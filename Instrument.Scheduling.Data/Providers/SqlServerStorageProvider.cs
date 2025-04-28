using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Instrument.Scheduling.Data.Providers;

public class SqlServerStorageProvider<T> : IStorageProvider<T> where T : class
{
    private readonly SchedulerDbContext _context;
    private readonly DbSet<T> _dbSet;
    private readonly ILogger<SqlServerStorageProvider<T>> _logger;

    public SqlServerStorageProvider(
        SchedulerDbContext context,
        ILogger<SqlServerStorageProvider<T>> logger)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _logger = logger;
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        try
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all entities of type {EntityType}", typeof(T).Name);
            throw new StorageProviderException("GetAll", ex);
        }
    }

    public async Task<T?> GetByIdAsync(string id)
    {
        try
        {
            return await _dbSet.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get entity of type {EntityType} with ID {Id}",
                typeof(T).Name, id);
            throw new StorageProviderException("GetById", ex);
        }
    }

    public async Task AddAsync(T entity)
    {
        try
        {
            await _dbSet.AddAsync(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add entity of type {EntityType}", typeof(T).Name);
            throw new StorageProviderException("Add", ex);
        }
    }

    public async Task UpdateAsync(T entity)
    {
        try
        {
            _dbSet.Update(entity);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update entity of type {EntityType}", typeof(T).Name);
            throw new StorageProviderException("Update", ex);
        }
    }

    public async Task DeleteAsync(string id)
    {
        try
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete entity of type {EntityType} with ID {Id}",
                typeof(T).Name, id);
            throw new StorageProviderException("Delete", ex);
        }
    }

    public async Task SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save changes for entity type {EntityType}", typeof(T).Name);
            throw new StorageProviderException("SaveChanges", ex);
        }
    }
}