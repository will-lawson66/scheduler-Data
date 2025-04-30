using Instrument.Scheduling.Data.Configuration;
using Instrument.Scheduling.Data.Providers;

namespace Instrument.Scheduling.Data.UT;
public class ConfigurationTests
{
    [Fact]
    public void StorageConfiguration_SqliteProvider_InitializesCorrectly()
    {
        // Arrange
        var config = new StorageConfiguration
        {
            Provider = StorageProviderType.SQLite,
            ConnectionString = "Data Source=test.db"
        };
        
        // Act & Assert
        Assert.Equal(StorageProviderType.SQLite, config.Provider);
        Assert.Equal("Data Source=test.db", config.ConnectionString);
    }

    [Fact]
    public void StorageConfiguration_SqlServerProvider_InitializesCorrectly()
    {
        // Arrange
        var config = new StorageConfiguration
        {
            Provider = StorageProviderType.SqlServer,
            ConnectionString = "Server=test;Database=TestDb;Trusted_Connection=True;"
        };
        
        // Act & Assert
        Assert.Equal(StorageProviderType.SqlServer, config.Provider);
        Assert.Equal("Server=test;Database=TestDb;Trusted_Connection=True;", config.ConnectionString);
    }

    [Fact]
    public void StorageProviderType_HasExpectedValues()
    {
        // Act & Assert
        Assert.Equal(1, (int)StorageProviderType.SQLite);
        Assert.Equal(2, (int)StorageProviderType.SqlServer);
    }
}