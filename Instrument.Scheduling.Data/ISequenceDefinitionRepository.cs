using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Scheduler.DataLayer.Entities;

namespace Scheduler.DataLayer.Interfaces
{
    public interface ISequenceDefinitionRepository
    {
        Task<IEnumerable<SequenceDefinition>> GetAllAsync();
        Task<SequenceDefinition?> GetByIdAsync(string id);
        Task<IQueryable<SequenceDefinition>> GetQueryableAsync();
        Task AddAsync(SequenceDefinition sequence);
        Task UpdateAsync(SequenceDefinition sequence);
        Task DeleteAsync(string id);
        Task SaveChangesAsync();
    }
}
