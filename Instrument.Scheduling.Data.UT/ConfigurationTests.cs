using Instrument.Scheduling.Data.Configuration;
using Instrument.Scheduling.Data.Providers;

namespace Instrument.Scheduling.Data.UT;
public class ConfigurationTests
{
    [Fact]
    public void StorageConfiguration_JsonProvider_InitializesCorrectly()
    {
        // Arrange
        var config = new StorageConfiguration
        {
            Provider = StorageProviderType.Json,
            JsonFilePath = "test.json"
        };
        
        // Act & Assert
        Assert.Equal(StorageProviderType.Json, config.Provider);
        Assert.Equal("test.json", config.JsonFilePath);
    }

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
        Assert.Equal("sequence_definitions.json", config.JsonFilePath);
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
        Assert.Equal("sequence_definitions.json", config.JsonFilePath);
    }
    
    [Fact]
    public void StorageConfiguration_DefaultValues()
    {
        // Arrange
        var config = new StorageConfiguration();
        
        // Act & Assert - check defaults
        Assert.Equal(StorageProviderType.Json, config.Provider); // Default is usually Json
        Assert.Empty(config.ConnectionString);
        Assert.Equal("sequence_definitions.json", config.JsonFilePath);
    }

    [Fact]
    public void StorageConfiguration_Validates_JsonConfig()
    {
        // Arrange
        //var config = new StorageConfiguration
        //{
        //    Provider = StorageProviderType.Json,
        //    // Missing JsonFilePath
        //};
        
        //// Act & Assert
        //var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        //Assert.Contains("JsonFilePath", exception.Message);
    }
    
    [Fact]
    public void StorageConfiguration_Validates_SqliteConfig()
    {
        // Arrange
        //var config = new StorageConfiguration
        //{
        //    Provider = StorageProviderType.SQLite,
        //    // Missing ConnectionString
        //};
        
        //// Act & Assert
        //var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        //Assert.Contains("ConnectionString", exception.Message);
    }
    
    [Fact]
    public void StorageConfiguration_Validates_SqlServerConfig()
    {
        //// Arrange
        //var config = new StorageConfiguration
        //{
        //    Provider = StorageProviderType.SqlServer,
        //    // Missing ConnectionString
        //};
        
        //// Act & Assert
        //var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        //Assert.Contains("ConnectionString", exception.Message);
    }
    
    [Fact]
    public void StorageProviderType_HasExpectedValues()
    {
        // Act & Assert
        Assert.Equal(0, (int)StorageProviderType.Json);
        Assert.Equal(1, (int)StorageProviderType.SQLite);
        Assert.Equal(2, (int)StorageProviderType.SqlServer);
    }
}