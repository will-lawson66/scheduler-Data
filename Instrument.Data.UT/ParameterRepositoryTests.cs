using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;
using Instrument.Data.Exceptions;
using Instrument.Data.Repository;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Data.UT;

public class ParameterRepositoryTests : IDisposable
{
    private readonly SchedulerDbContext _dbContext;
    private readonly ParameterRepository _repository;

    public ParameterRepositoryTests()
    {
        var dbName = $"TestDB_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        
        _dbContext = new SchedulerDbContext(options);
        _repository = new ParameterRepository(_dbContext);
    }

    public void Dispose()
    {
        // Clean up database after test
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
