namespace Instrument\.Data.Configuration;

/// <summary>
/// Configuration for the storage provider
/// </summary>
public class StorageConfiguration
{
    /// <summary>
    /// The type of storage provider to use
    /// </summary>
    public StorageProviderType Provider { get; set; } = StorageProviderType.SQLite;

    /// <summary>
    /// Path to directory for JSON import/export
    /// </summary>
    public string JsonDataPath { get; set; } = "./data";

    /// <summary>
    /// Connection string (used for SQLite and SqlServer providers)
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}