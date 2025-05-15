using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Data.Repository;

/// <summary>
/// Repository for parameters
/// </summary>
public class ParameterRepository : Repository<Parameter>, IParameterRepository
{
    /// <summary>
    /// Creates a new parameter repository
    /// </summary>
    /// <param name="dbContext">Database context</param>
    public ParameterRepository(SchedulerDbContext dbContext)
        : base(dbContext)
    {
    }
}