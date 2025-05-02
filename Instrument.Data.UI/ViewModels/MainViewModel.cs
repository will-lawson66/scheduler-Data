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
        private bool _isSequencesSelected;

        [ObservableProperty]
        private bool _isParametersSelected;

        [ObservableProperty]
        private bool _isRangesSelected;

        [ObservableProperty]
        private bool _isResourcesSelected;

        [ObservableProperty]
        private bool _isSequenceGroupsSelected;

        [ObservableProperty]
        private bool _isVisualizerSelected;

        public MainViewModel(NavigationService navigationService, ILogger<MainViewModel> logger)
        {
            _navigationService = navigationService;
            _logger = logger;
        }

        [RelayCommand]
        private void NavigateToSequences()
        {
            ClearSelections();
            IsSequencesSelected = true;
            _navigationService.NavigateTo<SequencesView>();
            _logger.LogInformation("Navigated to Sequences view");
        }

        [RelayCommand]
        private void NavigateToParameters()
        {
            ClearSelections();
            IsParametersSelected = true;
            _navigationService.NavigateTo<ParametersView>();
            _logger.LogInformation("Navigated to Parameters view");
        }

        [RelayCommand]
        private void NavigateToRanges()
        {
            ClearSelections();
            IsRangesSelected = true;
            _navigationService.NavigateTo<RangesView>();
            _logger.LogInformation("Navigated to Ranges view");
        }

        [RelayCommand]
        private void NavigateToResources()
        {
            ClearSelections();
            IsResourcesSelected = true;
            _navigationService.NavigateTo<ResourcesView>();
            _logger.LogInformation("Navigated to Resources view");
        }

        [RelayCommand]
        private void NavigateToSequenceGroups()
        {
            ClearSelections();
            IsSequenceGroupsSelected = true;
            _navigationService.NavigateTo<SequenceGroupsView>();
            _logger.LogInformation("Navigated to Sequence Groups view");
        }

        [RelayCommand]
        private void NavigateToVisualizer()
        {
            ClearSelections();
            IsVisualizerSelected = true;
            // To be implemented later when we add the visualizer
            _logger.LogInformation("Navigated to Visualizer view");
        }

        private void ClearSelections()
        {
            IsSequencesSelected = false;
            IsParametersSelected = false;
            IsRangesSelected = false;
            IsResourcesSelected = false;
            IsSequenceGroupsSelected = false;
            IsVisualizerSelected = false;
        }
    }
}
