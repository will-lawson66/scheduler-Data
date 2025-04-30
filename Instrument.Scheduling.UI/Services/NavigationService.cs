using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace Instrument.Scheduling.UI.Services
{
    public class NavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        public ContentControl? ContentControl { get; set; }
        private MainWindow? _mainWindow;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Initialize(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void NavigateTo<T>() where T : UserControl
        {
            if (ContentControl is null)
                return;

            var view = _serviceProvider.GetRequiredService<T>();
            ContentControl.Content = view;
        }

        public void NavigateTo<T>(object parameter) where T : UserControl
        {
            if (ContentControl is null)
                return;

            var view = _serviceProvider.GetRequiredService<T>();
            
            if (view.DataContext is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedTo(parameter);
            }
            
            ContentControl.Content = view;
        }
    }

    public interface INavigationAware
    {
        void OnNavigatedTo(object parameter);
    }
}
