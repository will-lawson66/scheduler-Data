using Instrument.Scheduling.Data.Configuration;
using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Initialization;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Providers;
using Instrument.Scheduling.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Instrument.Scheduling.Data.UT;
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSchedulerDataLayer_RegistersSqliteServices_WhenSqliteProviderSpecified()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add logging services required by our updated services
        services.AddLogging(builder => builder.AddConsole());
        
        var config = new StorageConfiguration
        {
            Provider = StorageProviderType.SQLite,
            ConnectionString = "Data Source=:memory:"
        };
        
        // Act
        services.AddSchedulerDataLayer(config);
        
        // Assert
        var provider = services.BuildServiceProvider();
        
        // Verify service registrations
        var dbContext = provider.GetService<SchedulerDbContext>();
        var sequenceProvider = provider.GetService<IStorageProvider<Sequence>>();
        var parameterProvider = provider.GetService<IStorageProvider<Parameter>>();
        var sequenceParameterProvider = provider.GetService<IStorageProvider<SequenceParameter>>();
        var rangeProvider = provider.GetService<IStorageProvider<Entities.Range>>();
        var rangeValueProvider = provider.GetService<IStorageProvider<RangeValue>>();
        var resourceProvider = provider.GetService<IStorageProvider<Resource>>();
        var unitOfWork = provider.GetService<IUnitOfWork>();
        
        Assert.NotNull(dbContext);
        Assert.NotNull(sequenceProvider);
        Assert.NotNull(parameterProvider);
        Assert.NotNull(sequenceParameterProvider);
        Assert.NotNull(rangeProvider);
        Assert.NotNull(rangeValueProvider);
        Assert.NotNull(resourceProvider);
        Assert.NotNull(unitOfWork);
        
        Assert.IsType<SqliteStorageProvider<Sequence>>(sequenceProvider);
        Assert.IsType<SqliteStorageProvider<Parameter>>(parameterProvider);
        Assert.IsType<SqliteSequenceParameterProvider>(sequenceParameterProvider);
        Assert.IsType<SqliteStorageProvider<Entities.Range>>(rangeProvider);
        Assert.IsType<SqliteStorageProvider<RangeValue>>(rangeValueProvider);
        Assert.IsType<SqliteStorageProvider<Resource>>(resourceProvider);
        Assert.IsType<UnitOfWork>(unitOfWork);
    }
    
    [Fact]
    public void AddSchedulerDataLayer_RegistersRepositories()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add logging services required by our updated services
        services.AddLogging(builder => builder.AddConsole());
        
        var config = new StorageConfiguration
        {
            Provider = StorageProviderType.SQLite,
            JsonExportPath = "test.json"
        };
        
        // Act
        services.AddSchedulerDataLayer(config);
        
        // Assert
        var provider = services.BuildServiceProvider();
        
        var sequenceRepo = provider.GetService<ISequenceRepository>();
        var parameterRepo = provider.GetService<IParameterRepository>();
        var rangeRepo = provider.GetService<IRangeRepository>();
        var rangeValueRepo = provider.GetService<IRangeValueRepository>();
        var resourceRepo = provider.GetService<IResourceRepository>();
        
        Assert.NotNull(sequenceRepo);
        Assert.NotNull(parameterRepo);
        Assert.NotNull(rangeRepo);
        Assert.NotNull(rangeValueRepo);
        Assert.NotNull(resourceRepo);
    }
    
    [Fact]
    public void AddCleanupServices_RegistersCleanupServices()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add required services first
        services.AddLogging(builder => builder.AddConsole());
        
        // Add a mock DbContext for dependency resolution
        services.AddDbContext<SchedulerDbContext>(options => 
            options.UseInMemoryDatabase("TestDb"));
            
        // Add a mock UnitOfWork
        services.AddScoped<IUnitOfWork>(sp => {
            var mockUnitOfWork = new Mock<IUnitOfWork>().Object;
            return mockUnitOfWork;
        });
        
        // Act
        services.AddCleanupServices();
        
        // Assert
        var provider = services.BuildServiceProvider();
        var dbCleanupService = provider.GetService<DatabaseCleanupService>();
        var jsonCleanupService = provider.GetService<JsonDataCleanupService>();
        
        Assert.NotNull(dbCleanupService);
        Assert.NotNull(jsonCleanupService);
    }
    
    [Fact]
    public void AddSchedulerDataLayer_ThrowsException_WhenConfigIsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act & Assert
        Assert.Throws<NullReferenceException>(() => services.AddSchedulerDataLayer(null));
    }
    
    [Fact]
    public void AddSchedulerDataWithInitialization_ThrowsException_WhenConfigIsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act & Assert
        Assert.Throws<NullReferenceException>(() => services.AddSchedulerDataWithInitialization(null));
    }
}
