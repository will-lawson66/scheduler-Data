using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Instrument.Scheduling.Data.Providers;

/// <summary>
/// Provides storage operations for entities in SQL Server
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public class SqlServerStorageProvider<T> : IStorageProvider<T> where T : class
{
    private readonly SchedulerDbContext _context;
    private readonly DbSet<T> _dbSet;
    private readonly ILogger<SqlServerStorageProvider<T>> _logger;
    private readonly string _entityTypeName;

    public SqlServerStorageProvider(
        SchedulerDbContext context,
        ILogger<SqlServerStorageProvider<T>> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<T>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _entityTypeName = typeof(T).Name;
    }

    /// <summary>
    /// Gets all entities
    /// </summary>
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        try
        {
            _logger.LogDebug("Retrieving all entities of type {EntityType}", _entityTypeName);
            return await _dbSet.AsNoTracking().ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all entities of type {EntityType}", _entityTypeName);
            throw new StorageProviderException("GetAllAsync", ex);
        }
    }

    /// <summary>
    /// Gets an entity by its ID
    /// </summary>
    /// <param name="id">Entity ID</param>
    public async Task<T?> GetByIdAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentNullException(nameof(id));

        try
        {
            _logger.LogDebug("Retrieving entity of type {EntityType} with ID {Id}", _entityTypeName, id);

            return await _dbSet.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get entity of type {EntityType} with ID {Id}", _entityTypeName, id);
            throw new StorageProviderException("GetByIdAsync", ex);
        }
    }

    /// <summary>
    /// Adds a new entity
    /// </summary>
    /// <param name="entity">Entity to add</param>
    public async Task AddAsync(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            _logger.LogDebug("Adding entity of type {EntityType}", _entityTypeName);
            await _dbSet.AddAsync(entity);
            _logger.LogInformation("Added entity of type {EntityType}", _entityTypeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add entity of type {EntityType}", _entityTypeName);
            throw new StorageProviderException("AddAsync", ex);
        }
    }

    /// <summary>
    /// Updates an existing entity
    /// </summary>
    /// <param name="entity">Entity with updated values</param>
    public async Task UpdateAsync(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            _logger.LogDebug("Updating entity of type {EntityType}", _entityTypeName);

            // Detach any existing tracked entity to avoid conflicts
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty != null)
            {
                var id = idProperty.GetValue(entity)?.ToString();
                if (!string.IsNullOrEmpty(id))
                {
                    var existingEntity = await _dbSet.FindAsync(id);
                    if (existingEntity != null)
                    {
                        _context.Entry(existingEntity).State = EntityState.Detached;
                    }
                    else
                    {
                        _logger.LogWarning("Entity of type {EntityType} with ID {Id} not found for update",
                            _entityTypeName, id);
                        throw new EntityNotFoundException(_entityTypeName, id);
                    }
                }
            }

            _dbSet.Update(entity);
            _logger.LogInformation("Updated entity of type {EntityType}", _entityTypeName);
        }
        catch (EntityNotFoundException)
        {
            throw; // Re-throw known entity not found exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update entity of type {EntityType}", _entityTypeName);
            throw new StorageProviderException("UpdateAsync", ex);
        }
    }

    /// <summary>
    /// Deletes an entity by its ID
    /// </summary>
    /// <param name="id">ID of the entity to delete</param>
    public async Task DeleteAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentNullException(nameof(id));

        try
        {
            _logger.LogDebug("Deleting entity of type {EntityType} with ID {Id}", _entityTypeName, id);

            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                _logger.LogInformation("Deleted entity of type {EntityType} with ID {Id}", _entityTypeName, id);
            }
            else
            {
                _logger.LogWarning("Entity of type {EntityType} with ID {Id} not found for deletion",
                    _entityTypeName, id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete entity of type {EntityType} with ID {Id}", _entityTypeName, id);
            throw new StorageProviderException("DeleteAsync", ex);
        }
    }

    /// <summary>
    /// Saves all pending changes to the database
    /// </summary>
    public async Task SaveChangesAsync()
    {
        try
        {
            _logger.LogDebug("Saving changes to the database for entity type {EntityType}", _entityTypeName);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Saved changes to the database for entity type {EntityType}", _entityTypeName);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict when saving changes for entity type {EntityType}", _entityTypeName);
            throw new StorageProviderException("SaveChangesAsync (Concurrency conflict)", ex);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error when saving changes for entity type {EntityType}", _entityTypeName);
            throw new StorageProviderException("SaveChangesAsync (Update error)", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save changes for entity type {EntityType}", _entityTypeName);
            throw new StorageProviderException("SaveChangesAsync", ex);
        }
    }
}
