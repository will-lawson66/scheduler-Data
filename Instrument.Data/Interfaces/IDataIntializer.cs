using System;
using System.Threading.Tasks;

namespace Instrument\.Data.Initialization;

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
    /// Seeds the data storage with default data if empty
    /// </summary>
    Task<bool> SeedDefaultDataAsync();

    /// <summary>
    /// Gets the storage status message
    /// </summary>
    Task<string> GetStatusMessageAsync();
}