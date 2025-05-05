using Instrument.Data.Configuration;
using Instrument.Data.UI.Services;
using Instrument.Data.UI.ViewModels;
using Instrument.Data.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace Instrument.Data.UI;

public partial class App : Application
{
    private IHost _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        _host = Host.CreateDefaultBuilder(e.Args)
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

                // Register services
                services.AddSchedulerDataLayer(storageConfig);
                services.AddDataInitialization();
                services.AddJsonCommands();

                // UI services
                services.AddSingleton<NavigationService>();
                services.AddSingleton<DialogService>();

                // ViewModels
                RegisterViewModels(services);

                // Views
                RegisterViews(services);
            })
            .Build();

        _host.Start();

        // Global exception handling
        this.DispatcherUnhandledException += (s, ex) =>
        {
            var logger = _host.Services.GetRequiredService<ILogger<App>>();
            logger.LogError(ex.Exception, "Unhandled exception");
            MessageBox.Show($"An error occurred: {ex.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };

        // Start main window
        var navigationService = _host.Services.GetRequiredService<NavigationService>();
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        navigationService.Initialize(mainWindow);
        mainWindow.Show();
        navigationService.NavigateTo<SequencesView>();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private void RegisterViewModels(IServiceCollection services)
    {
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

    private void RegisterViews(IServiceCollection services)
    {
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
