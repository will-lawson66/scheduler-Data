using Instrument.Scheduling.Data;
using Instrument.Scheduling.Data.Configuration;
using Instrument.Scheduling.Data.Providers;
using Instrument.Scheduling.UI.Services;
using Instrument.Scheduling.UI.ViewModels;
using Instrument.Scheduling.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace Instrument.Scheduling.UI
{
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(ConfigureServices)
                .Build();
        }

        private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            // Configure Logging
            services.AddLogging(configure => 
            {
                configure.AddDebug();
                configure.SetMinimumLevel(LogLevel.Information);
            });

            // Configure SQLite storage
            var storageConfig = new StorageConfiguration
            {
                Provider = StorageProviderType.SQLite,
                ConnectionString = "Data Source=SchedulerData.db"
            };

            // Add Data Layer
            services.AddSchedulerDataLayer(storageConfig);
            
            // Add Data Initialization
            services.AddDataInitialization();
            
            // Add JSON Commands for import/export
            services.AddJsonCommands();
            
            // Add UI Services
            services.AddSingleton<NavigationService>();
            services.AddSingleton<DialogService>();
            
            // Add ViewModels
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
            
            // Add Views
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
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            var navigationService = _host.Services.GetRequiredService<NavigationService>();
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            
            navigationService.Initialize(mainWindow);
            mainWindow.Show();

            // Navigate to the Sequences view by default
            navigationService.NavigateTo<SequencesView>();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
            }

            base.OnExit(e);
        }
    }
}
