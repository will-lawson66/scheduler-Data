using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Instrument.Data.UI.Services;
using Instrument.Data.UI.Views;
using Microsoft.Extensions.Logging;

namespace Instrument.Data.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly NavigationService _navigationService;
        private readonly ILogger<MainViewModel> _logger;

        [ObservableProperty]
        private int _selectedViewIndex = 0;

        public MainViewModel(NavigationService navigationService, ILogger<MainViewModel> logger)
        {
            _navigationService = navigationService;
            _logger = logger;
        }
        
        partial void OnSelectedViewIndexChanged(int value)
        {
            switch(value)
            {
                case 0:
                    NavigateToSequences();
                    break;
                case 1:
                    NavigateToParameters();
                    break;
                case 2:
                    NavigateToRanges();
                    break;
                case 3:
                    NavigateToResources();
                    break;
                case 4:
                    NavigateToSequenceGroups();
                    break;
            }
        }

        [RelayCommand]
        private void NavigateToSequences()
        {
            _navigationService.NavigateTo<SequencesView>();
            _logger.LogInformation("Navigated to Sequences view");
        }

        [RelayCommand]
        private void NavigateToParameters()
        {
            _navigationService.NavigateTo<ParametersView>();
            _logger.LogInformation("Navigated to Parameters view");
        }

        [RelayCommand]
        private void NavigateToRanges()
        {
            _navigationService.NavigateTo<RangesView>();
            _logger.LogInformation("Navigated to Ranges view");
        }

        [RelayCommand]
        private void NavigateToResources()
        {
            _navigationService.NavigateTo<ResourcesView>();
            _logger.LogInformation("Navigated to Resources view");
        }

        [RelayCommand]
        private void NavigateToSequenceGroups()
        {
            _navigationService.NavigateTo<SequenceGroupsView>();
            _logger.LogInformation("Navigated to Sequence Groups view");
        }

        [RelayCommand]
        private void NavigateToVisualizer()
        {
            _navigationService.NavigateTo<RelationshipVisualizerView>();
            _logger.LogInformation("Navigated to Visualizer view");
        }
        
        [RelayCommand]
        private void Import()
        {
            // Will implement import functionality
            _logger.LogInformation("Import command executed");
        }
        
        [RelayCommand]
        private void Export()
        {
            // Will implement export functionality
            _logger.LogInformation("Export command executed");
        }


    }
}
