using Instrument.Data.UI.Services;
using Instrument.Data.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;

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

        private void NavigationItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // The SelectedIndex binding in XAML will handle the navigation
            // We don't need to do anything here as the ViewModel handles the actual navigation
            // based on the SelectedViewIndex property
        }

        private void ToolsItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var selectedItem = e.AddedItems[0] as ListBoxItem;
                
                if (selectedItem == VisualizerItem)
                {
                    _viewModel.NavigateToVisualizerCommand.Execute(null);
                }
                else if (selectedItem == ImportItem)
                {
                    _viewModel.ImportCommand.Execute(null);
                }
                else if (selectedItem == ExportItem)
                {
                    _viewModel.ExportCommand.Execute(null);
                }
                
                // Clear selection so item can be reselected
                ((ListBox)sender).SelectedItem = null;
            }
        }
    }
}
