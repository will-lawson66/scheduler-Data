using Avalonia.Controls;
using Instrument.Data.Avalonia.Services;
using Instrument.Data.Avalonia.ViewModels.Base;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Instrument.Data.Avalonia.ViewModels
{
    /// <summary>
    /// Main window view model that handles navigation and global application state
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IThemeService _themeService;
        private readonly ILogger<MainWindowViewModel> _logger;
        
        private int _selectedViewIndex;
        
        /// <summary>
        /// Navigation service for the application
        /// </summary>
        public INavigationService NavigationService { get; }
        
        /// <summary>
        /// Dialog service for the application
        /// </summary>
        public IDialogService DialogService { get; }
        
        /// <summary>
        /// Selected view index in the navigation menu
        /// </summary>
        public int SelectedViewIndex
        {
            get => _selectedViewIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedViewIndex, value);
        }
        
        // Commands
        public ReactiveCommand<Unit, Unit> ToggleThemeCommand { get; }
        
        public MainWindowViewModel(
            INavigationService navigationService,
            IDialogService dialogService,
            IThemeService themeService,
            ILogger<MainWindowViewModel> logger)
        {
            NavigationService = navigationService;
            DialogService = dialogService;
            _themeService = themeService;
            _logger = logger;
            
            Title = "Instrument Data Manager";
            
            // Create the theme toggle command
            ToggleThemeCommand = ReactiveCommand.Create(() => _themeService.ToggleTheme());
        }
        
        protected override void HandleActivation()
        {
            // Default navigation to Sequences
            SelectedViewIndex = 0;
        }
        
        protected override void SetupActivationHandlers(CompositeDisposable disposables)
        {
            // Navigate to default view when SelectedViewIndex changes
            this.WhenAnyValue(x => x.SelectedViewIndex)
                .Subscribe(index => NavigateBasedOnIndex(index))
                .DisposeWith(disposables);
        }
        
        /// <summary>
        /// Navigate to a view based on the selected index
        /// </summary>
        /// <param name="index">Index of the view to navigate to</param>
        private void NavigateBasedOnIndex(int index)
        {
            try
            {
                switch (index)
                {
                    case 0:
                        NavigationService.NavigateTo<SequencesViewModel>();
                        break;
                    case 1:
                        NavigationService.NavigateTo<SequenceGroupsViewModel>();
                        break;
                    case 2:
                        NavigationService.NavigateTo<ParametersViewModel>();
                        break;
                    case 3:
                        NavigationService.NavigateTo<RangesViewModel>();
                        break;
                    case 4:
                        NavigationService.NavigateTo<ResourcesViewModel>();
                        break;
                    case 5:
                        NavigationService.NavigateTo<RelationshipVisualizerViewModel>();
                        break;
                    default:
                        NavigationService.NavigateTo<SequencesViewModel>();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to view with index {Index}", index);
                DialogService.ShowErrorAsync("Navigation Error", $"Failed to navigate to the selected view: {ex.Message}").ConfigureAwait(false);
            }
        }
    }
}
