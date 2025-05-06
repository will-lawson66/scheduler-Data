using Avalonia.Controls;
using Instrument.Data.Avalonia.Services;
using Instrument.Data.Avalonia.ViewModels.Base;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Instrument.Data.Avalonia.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<MainWindowViewModel> _logger;
        
        private int _selectedViewIndex;
        private object _currentView;
        
        public int SelectedViewIndex
        {
            get => _selectedViewIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedViewIndex, value);
        }
        
        public object CurrentView
        {
            get => _currentView;
            set => this.RaiseAndSetIfChanged(ref _currentView, value);
        }
        
        public MainWindowViewModel(
            INavigationService navigationService,
            IDialogService dialogService,
            ILogger<MainWindowViewModel> logger)
        {
            _navigationService = navigationService;
            _dialogService = dialogService;
            _logger = logger;
            
            Title = "Instrument Data Manager";
            
            // Initialize navigation with the ContentControl that will host views
            this.WhenActivated(disposables =>
            {
                _navigationService.Initialize(this, view => CurrentView = view);
                
                // Navigate to default view when SelectedViewIndex changes
                this.WhenAnyValue(x => x.SelectedViewIndex)
                    .Subscribe(index => NavigateBasedOnIndex(index))
                    .DisposeWith(disposables);
                
                // Default navigation to Sequences
                SelectedViewIndex = 0;
            });
        }
        
        private void NavigateBasedOnIndex(int index)
        {
            try
            {
                switch (index)
                {
                    case 0:
                        _navigationService.NavigateTo<SequencesViewModel>();
                        break;
                    case 1:
                        _navigationService.NavigateTo<SequenceGroupsViewModel>();
                        break;
                    case 2:
                        _navigationService.NavigateTo<ParametersViewModel>();
                        break;
                    case 3:
                        _navigationService.NavigateTo<RangesViewModel>();
                        break;
                    case 4:
                        _navigationService.NavigateTo<ResourcesViewModel>();
                        break;
                    case 5:
                        _navigationService.NavigateTo<RelationshipVisualizerViewModel>();
                        break;
                    default:
                        _navigationService.NavigateTo<SequencesViewModel>();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to view with index {Index}", index);
                _dialogService.ShowError("Navigation Error", $"Failed to navigate to the selected view: {ex.Message}");
            }
        }
    }
}
