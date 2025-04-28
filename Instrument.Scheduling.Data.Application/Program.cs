using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Instrument.Scheduling.Data;
using Instrument.Scheduling.Data.Configuration;
using Instrument.Scheduling.Data.Initialization;
using Instrument.Scheduling.Data.Services;
using Instrument.Scheduling.Data.DataContext;
using Microsoft.EntityFrameworkCore;
using Instrument.Scheduling.Data.Providers;


// Set up configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

// Configure services
var services = new ServiceCollection();

// Add logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Get storage configuration
var storageConfig = new StorageConfiguration();
configuration.GetSection("DataStorage").Bind(storageConfig);

// Add data services with initialization
services.AddSchedulerDataWithInitialization(storageConfig);
services.AddCleanupServices();

// Build service provider
using var serviceProvider = services.BuildServiceProvider();

try
{
    // Create data initializer
    var factory = serviceProvider.GetRequiredService<DataInitializerFactory>();
    var initializer = factory.CreateInitializer(storageConfig);

    // Check if storage exists
    bool exists = await initializer.ExistsAsync();
    System.Console.WriteLine($"Storage exists: {exists}");

    if (!exists)
    {
        System.Console.WriteLine("Initializing storage...");
        await initializer.InitializeAsync();
        System.Console.WriteLine("Storage initialized successfully");

        // Apply migrations for database storage
        bool migrationsApplied = await initializer.MigrateAsync();
        if (migrationsApplied)
        {
            System.Console.WriteLine("Migrations applied successfully");
        }

        // Seed default data
        System.Console.WriteLine("Seeding default data...");
        bool seeded = await initializer.SeedDefaultDataAsync();
        if (seeded)
        {
            System.Console.WriteLine("Default data seeded successfully");
        }
        else
        {
            System.Console.WriteLine("Data already exists, no seeding needed");
        }
    }

    // Get storage status
    string status = await initializer.GetStatusMessageAsync();
    System.Console.WriteLine("Storage status:");
    System.Console.WriteLine(status);
}
catch (Exception ex)
{
    System.Console.WriteLine($"Error: {ex.Message}");
    System.Console.WriteLine(ex.StackTrace);
}

Console.WriteLine($"1. Clear all data provider {storageConfig.Provider.ToString()}");
Console.WriteLine("2. Exit");
Console.WriteLine();
Console.Write("Enter your choice (1-2): ");

var key = Console.ReadKey();
Console.WriteLine();

switch (key.KeyChar)
{
    case '1':
        Console.WriteLine("Clearing all data...");
        switch (storageConfig.Provider)
        {
            case StorageProviderType.Json:
                var jsonCleanupService = serviceProvider.GetRequiredService<JsonDataCleanupService>();
                jsonCleanupService.ClearAllData();
                break;

            default:
                var dBCleanupService = serviceProvider.GetRequiredService<DatabaseCleanupService>();
                await dBCleanupService.ClearAllDataAsync();
                break;
        }

        break;

    case '2':
        break;

    default:
        break;
}



System.Console.WriteLine("Press any key to exit...");
System.Console.ReadKey();