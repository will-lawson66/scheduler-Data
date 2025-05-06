using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Instrument.Data.Avalonia.ViewModels;
using Instrument.Data.Avalonia.Views.Base;
using ReactiveUI;
using System;
using System.Reactive.Disposables;

namespace Instrument.Data.Avalonia.Views
{
    public partial class SequencesView : ReactiveViewBase<SequencesViewModel>
    {
        // References to controls that need to be bound programmatically
        private ListBox _sequencesList;
        
        public SequencesView()
        {
            InitializeComponent();
            
            // Find controls by name for programmatic binding
            _sequencesList = this.FindControl<ListBox>("SequencesList");
        }
        
        protected override void HandleActivation()
        {
            // This will be called when the view is attached to the visual tree
            // and has a valid DataContext (ViewModel)
            if (ViewModel != null)
            {
                // Trigger loading data when activated
                ViewModel.LoadSequencesCommand.Execute().Subscribe();
            }
        }
        
        protected override void SetupViewActivationHandlers(CompositeDisposable disposables)
        {
            // Setup additional bindings and event handlers that should be cleaned up on deactivation
            this.WhenAnyValue(x => x.ViewModel.SelectedSequence)
                .Subscribe(sequence => 
                {
                    // Handle selection changes
                })
                .DisposeWith(disposables);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
