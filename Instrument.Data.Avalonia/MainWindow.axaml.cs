using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Instrument.Data.Avalonia.Services;
using Instrument.Data.Avalonia.ViewModels;
using Instrument.Data.Avalonia.Views.Base;
using ReactiveUI;
using System;
using System.Reactive.Disposables;

namespace Instrument.Data.Avalonia
{
    public partial class MainWindow : ReactiveWindowBase<MainWindowViewModel>
    {
        // UI Components that need programmatic binding
        private ContentControl _contentRegion;
        
        public MainWindow()
        {
            InitializeComponent();
            
            // Find the content region control
            _contentRegion = this.FindControl<ContentControl>("ContentRegion");
        }
        
        protected override void HandleActivation()
        {
            // Initialize navigation with this window as the content host
            if (ViewModel != null && _contentRegion != null)
            {
                // Get navigation service from ViewModel
                var navigationService = ViewModel.NavigationService;
                
                // Initialize the navigation service with this window
                navigationService.Initialize(ViewModel, content => _contentRegion.Content = content);
            }
        }
        
        protected override void SetupWindowActivationHandlers(CompositeDisposable disposables)
        {
            // Additional window-level activation handlers can be added here
            // These will be disposed automatically when the window is closed
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
