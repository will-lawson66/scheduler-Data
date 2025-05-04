using System.Windows.Controls;
using Instrument.Data.UI.Services.Dialog;
using Instrument.Data.UI.Services.Interfaces;
using Instrument.Data.UI.Services.Navigation;
using Instrument.Data.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Instrument.Data.UI.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring UI services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds UI services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="navigationFrame">The frame used for navigation.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddUiServices(this IServiceCollection services, Frame navigationFrame)
        {
            // Register navigation service
            services.AddSingleton<INavigationService>(sp => {
                var navigationService = new NavigationService(sp, navigationFrame);
                
                // Register view mappings
                RegisterViewMappings(navigationService);
                
                return navigationService;
            });

            // Register dialog service
            services.AddSingleton<IDialogService, DialogService>();

            // Register ViewModels
            RegisterViewModels(services);

            return services;
        }

        private static void RegisterViewMappings(NavigationService navigationService)
        {
            // Register view mappings here
            // Example: navigationService.Register<HomeViewModel, HomeView>();
        }

        private static void RegisterViewModels(IServiceCollection services)
        {
            // Register ViewModels here
            // Example: services.AddTransient<HomeViewModel>();
        }
    }
}
