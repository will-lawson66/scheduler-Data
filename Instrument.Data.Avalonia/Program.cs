using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using Instrument.Data.Avalonia.Services;
using Instrument.Data.Avalonia.ViewModels;
using Instrument.Data.Configuration;
using Instrument.Data.Initialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Instrument.Data.Avalonia;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        // Initialize application services
        using (var scope = host.Services.CreateScope())
        {
            var initializer = scope.ServiceProvider.GetRequiredService<IDataInitializer>();
            initializer.InitializeAsync().Wait();
        }
        
        // Start Avalonia UI
        BuildAvaloniaApp(host).StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(IHost host)
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI()
            .With(new AvaloniaAppBuilderSettings { ServiceProvider = host.Services });

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
                // 1. Configure storage
                var storageConfig = new StorageConfiguration
                {
                    Provider = StorageProviderType.SQLite,
                    ConnectionString = "Data Source=Instrument.db"
                };
                
                // 2. Register data layer
                services.AddInstrumentData(storageConfig);
                services.AddDataInitialization();
                
                // 3. Register UI services
                RegisterServices(services);
                
                // 4. Register ViewModels
                RegisterViewModels(services);
            });

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IThemeService, ThemeService>();
        // Other services
    }

    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddTransient<MainWindowViewModel>();
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
        // Other ViewModels
    }
}

public class AvaloniaAppBuilderSettings
{
    public IServiceProvider ServiceProvider { get; set; }
}
