namespace Instrument.Data;

/// <summary>
/// Interface for data storage initialization
/// </summary>
public interface IDataInitializer
{
    /// <summary>
    /// Checks if the data storage exists
    /// </summary>
    Task<bool> ExistsAsync();

    /// <summary>
    /// Initializes the data storage
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Applies any pending migrations
    /// </summary>
    Task<bool> MigrateAsync();

    /// <summary>
    /// Gets the storage status message
    /// </summary>
    Task<string> GetStatusMessageAsync();
}