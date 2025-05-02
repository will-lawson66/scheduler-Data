namespace Instrument\.Data.Interfaces;

/// <summary>
/// Generic repository interface for data access
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Gets all entities
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();
    
    /// <summary>
    /// Gets an entity by its ID
    /// </summary>
    /// <param name="id">Entity ID</param>
    Task<T?> GetByIdAsync(string id);
    
    /// <summary>
    /// Gets a queryable for the entity
    /// </summary>
    Task<IQueryable<T>> GetQueryableAsync();
    
    /// <summary>
    /// Adds a new entity
    /// </summary>
    /// <param name="entity">Entity to add</param>
    Task AddAsync(T entity);
    
    /// <summary>
    /// Updates an existing entity
    /// </summary>
    /// <param name="entity">Entity to update</param>
    Task UpdateAsync(T entity);
    
    /// <summary>
    /// Deletes an entity by its ID
    /// </summary>
    /// <param name="id">Entity ID</param>
    Task DeleteAsync(string id);
    
    /// <summary>
    /// Saves changes to the data store
    /// </summary>
    Task SaveChangesAsync();
}
