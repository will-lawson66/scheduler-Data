using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Scheduling.Data.Repository;

/// <summary>
/// Repository for resources
/// </summary>
public class ResourceRepository : Repository<Resource>, IResourceRepository
{
    /// <summary>
    /// Creates a new resource repository
    /// </summary>
    /// <param name="dbContext">Database context</param>
    public ResourceRepository(SchedulerDbContext dbContext)
        : base(dbContext)
    {
    }
    
    /// <summary>
    /// Gets a resource by code
    /// </summary>
    /// <param name="code">