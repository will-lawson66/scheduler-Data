using Instrument.Data.Configuration;
using Instrument.Data.UI.Services;
using Instrument.Data.UI.ViewModels;
using Instrument.Data.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Windows;

namespace Instrument.Data.UI
{
    /// <summary>
    /// Alternative entry point for the application using top-level statements style.
    /// This can be used as an alternative to the App.xaml.cs entry point.
    /// </summary>
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(ConfigureServices)
                .Build();

            var app = new Application();
            var mainWindow = host.Services.GetRequiredService<MainWindow>();
            var navigationService = host.Services.GetRequiredService<NavigationService>();
            
            navigationService.Initialize(mainWindow);
            app.Run(mainWindow);
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
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
            services.AddTransient<RelationshipVisualizerViewModel>();
            
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
            services.AddTransient<RelationshipVisualizerView>();
            
            // Navigate to Sequences view by default - initialization is done in MainWindow
        }
    }
}
