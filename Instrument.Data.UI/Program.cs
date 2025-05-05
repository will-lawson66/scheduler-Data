using Instrument.Data.Configuration;
using Instrument.Data.Initialization;
using Instrument.Data.UI.Services;
using Instrument.Data.UI.ViewModels;
using Instrument.Data.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace Instrument.Data.UI;

/// <summary>
/// Application entry point using the host builder pattern for dependency injection
/// </summary>
public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        var app = new Application();
        
        // Set up exception handling for unhandled exceptions
        app.DispatcherUnhandledException += (s, e) =>
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(e.Exception, "Unhandled application exception");
            MessageBox.Show($"An error occurred: {e.Exception.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };
        
        try
        {
            // Initialize the application services
            InitializeApplication(host.Services).GetAwaiter().GetResult();
            
            // Create and run the main window
            var mainWindow = host.Services.GetRequiredService<MainWindow>();
            app.Run(mainWindow);
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetService<ILogger<Program>>();
            logger?.LogCritical(ex, "Application failed to start");
            MessageBox.Show($"Fatal error: {ex.Message}", 
                "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddDebug();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices((context, services) =>
            {
                // Configure storage
                var storageConfig = new StorageConfiguration
                {
                    Provider = StorageProviderType.SQLite,
                    ConnectionString = "Data Source=SchedulerData.db"
                };

                // Register core data services
                services.AddSchedulerDataLayer(storageConfig);
                services.AddDataInitialization();
                services.AddJsonCommands();
                
                // Register UI services
                services.AddSingleton<NavigationService>();
                services.AddSingleton<DialogService>();

                // Register ViewModels
                RegisterViewModels(services);
                
                // Register Views
                RegisterViews(services);
            });

    private static async Task InitializeApplication(IServiceProvider services)
    {
        // Initialize the database
        await InitializeDatabaseAsync(services);
        
        // Initialize the navigation service
        var mainWindow = services.GetRequiredService<MainWindow>();
        var navigationService = services.GetRequiredService<NavigationService>();
        navigationService.Initialize(mainWindow);
        
        // Set the default view to navigate to at startup
        navigationService.NavigateTo<SequencesView>();
    }

    private static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        try
        {
            logger.LogInformation("Initializing database...");
            
            // Create a scope for database initialization
            using var scope = services.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<DataInitializerFactory>();
            var storageConfig = new StorageConfiguration
            {
                Provider = StorageProviderType.SQLite,
                ConnectionString = "Data Source=SchedulerData.db"
            };

            var initializer = factory.CreateInitializer(storageConfig);
            
            // Create database if it doesn't exist
            if (!await initializer.ExistsAsync())
            {
                logger.LogInformation("Database doesn't exist. Creating...");
                await initializer.InitializeAsync();
                logger.LogInformation("Database created successfully");
            }
            
            // Apply any pending migrations
            bool migrationsApplied = await initializer.MigrateAsync();
            if (migrationsApplied)
            {
                logger.LogInformation("Database migrations applied successfully");
            }
            
            // Seed default data if needed
            bool dataSeeded = await initializer.SeedDefaultDataAsync();
            if (dataSeeded)
            {
                logger.LogInformation("Default data seeded successfully");
            }
            
            // Log database status
            var status = await initializer.GetStatusMessageAsync();
            logger.LogInformation("Database Status: {Status}", status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database initialization failed");
            throw new ApplicationException("Failed to initialize the database. See inner exception for details.", ex);
        }
    }

    private static void RegisterViewModels(IServiceCollection services)
    {
        // Register all ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<SequencesViewModel>();
        services.AddTransient<SequenceDetailViewModel>();
        services.AddTransient<ParametersViewModel>();
        services.AddTransient<ParameterDetailViewModel>();
        services.AddTransient<RangesViewModel>();
        services.AddTransient<RangeDetailViewModel>();
        services.AddTransient<ResourcesViewModel>();
        services.AddTransient<ResourceDetailViewModel>();
        services.AddTransient<SequenceGroupsViewModel>();
        services.AddTransient<SequenceGroupDetailViewModel>();
        services.AddTransient<RelationshipVisualizerViewModel>();
    }

    private static void RegisterViews(IServiceCollection services)
    {
        // Register all Views
        services.AddSingleton<MainWindow>();
        services.AddTransient<SequencesView>();
        services.AddTransient<SequenceDetailView>();
        services.AddTransient<ParametersView>();
        services.AddTransient<ParameterDetailView>();
        services.AddTransient<RangesView>();
        services.AddTransient<RangeDetailView>();
        services.AddTransient<ResourcesView>();
        services.AddTransient<ResourceDetailView>();
        services.AddTransient<SequenceGroupsView>();
        services.AddTransient<SequenceGroupDetailView>();
        services.AddTransient<RelationshipVisualizerView>();
    }
}