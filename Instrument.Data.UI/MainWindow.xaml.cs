using Instrument.Data.UI.Services;
using Instrument.Data.UI.ViewModels;
using System.Windows;

namespace Instrument.Data.UI
{
    public partial class MainWindow : Window
    {
        private readonly NavigationService _navigationService;
        private readonly MainViewModel _viewModel;

        public MainWindow(NavigationService navigationService, MainViewModel viewModel)
        {
            InitializeComponent();
            
            _navigationService = navigationService;
            _viewModel = viewModel;
            
            DataContext = _viewModel;
            _navigationService.ContentControl = MainContent;
        }
    }
}
