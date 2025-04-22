using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Scheduler.DataLayer.Interfaces
{
    public interface IStorageProvider<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(string id);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(string id);
        Task SaveChangesAsync();
    }
}
