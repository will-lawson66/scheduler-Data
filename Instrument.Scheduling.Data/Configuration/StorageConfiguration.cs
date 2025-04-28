using Instrument.Scheduling.Data.Providers;

namespace Instrument.Scheduling.Data.Configuration;

/// <summary>
/// Configuration for the storage provider
/// </summary>
public class StorageConfiguration
{
    /// <summary>
    /// The type of storage provider to use
    /// </summary>
    public StorageProviderType Provider { get; set; } = StorageProviderType.Json;

    /// <summary>
    /// Path to the JSON file (used for Json provider)
    /// </summary>
    public string JsonFilePath { get; set; } = "sequence_definitions.json";

    /// <summary>
    /// Connection string (used for SQLite and SqlServer providers)
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}