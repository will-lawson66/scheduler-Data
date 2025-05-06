using DynamicData;
using DynamicData.Binding;
using Instrument.Data.Avalonia.Services;
using Instrument.Data.Avalonia.ViewModels.Base;
using Instrument.Data.Entities;
using Instrument.Data.Services;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Instrument.Data.Avalonia.ViewModels
{
    /// <summary>
    /// ViewModel for the Sequences view that displays a list of all sequences
    /// </summary>
    public class SequencesViewModel : ViewModelBase, INavigationAware
    {
        private readonly SequenceService _sequenceService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<SequencesViewModel> _logger;
        
        // Using SourceList for better ReactiveUI integration
        private readonly SourceList<Sequence> _sequencesSource = new SourceList<Sequence>();
        private readonly ReadOnlyObservableCollection<Sequence> _sequences;
        private Sequence _selectedSequence;
        
        /// <summary>
        /// Observable collection of sequences for data binding
        /// </summary>
        public ReadOnlyObservableCollection<Sequence> Sequences => _sequences;
        
        /// <summary>
        /// Currently selected sequence
        /// </summary>
        public Sequence SelectedSequence
        {
            get => _selectedSequence;
            set => this.RaiseAndSetIfChanged(ref _selectedSequence, value);
        }
        
        /// <summary>
        /// Indicates if there are no sequences in the list
        /// </summary>
        public bool HasNoSequences => !Sequences.Any();
        
        // Commands
        public ReactiveCommand<Unit, IEnumerable<Sequence>> LoadSequencesCommand { get; }
        public ReactiveCommand<Unit, Unit> CreateSequenceCommand { get; }
        public ReactiveCommand<Sequence, Unit> ViewSequenceCommand { get; }
        public ReactiveCommand<Sequence, Unit> DeleteSequenceCommand { get; }
        
        public SequencesViewModel(
            SequenceService sequenceService,
            INavigationService navigationService,
            IDialogService dialogService,
            ILogger<SequencesViewModel> logger)
        {
            _sequenceService = sequenceService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _logger = logger;
            
            Title = "Sequences";
            
            // Connect the source list to the observable collection
            _sequencesSource.Connect()
                .Bind(out _sequences)
                .Subscribe();
            
            // Create load command with proper typing and error handling
            LoadSequencesCommand = ReactiveCommand.CreateFromTask(
                LoadSequencesAsync,
                this.IsNotLoading);
            
            // Command for creating a new sequence
            CreateSequenceCommand = ReactiveCommand.Create(() => 
                _navigationService.NavigateTo<SequenceDetailViewModel>("new"));
            
            // Command for viewing a sequence - takes a sequence parameter
            ViewSequenceCommand = ReactiveCommand.Create<Sequence, Unit>(
                sequence => 
                {
                    _navigationService.NavigateTo<SequenceDetailViewModel>(sequence.Id);
                    return Unit.Default;
                });
            
            // Command for deleting a sequence - takes a sequence parameter
            DeleteSequenceCommand = ReactiveCommand.CreateFromTask<Sequence, Unit>(
                DeleteSequenceAsync);
        }
        
        protected override void SetupActivationHandlers(CompositeDisposable disposables)
        {
            // Update HasNoSequences whenever the collection changes
            _sequencesSource.Connect()
                .WhenValueChanged(x => x.Count())
                .Subscribe(_ => this.RaisePropertyChanged(nameof(HasNoSequences)))
                .DisposeWith(disposables);
            
            // Subscribe to the load command to handle errors
            LoadSequencesCommand.ThrownExceptions
                .Subscribe(async ex => 
                {
                    _logger.LogError(ex, "Failed to load sequences");
                    await _dialogService.ShowErrorAsync("Error", $"Failed to load sequences: {ex.Message}");
                })
                .DisposeWith(disposables);
                
            // Subscribe to delete command exceptions
            DeleteSequenceCommand.ThrownExceptions
                .Subscribe(async ex => 
                {
                    _logger.LogError(ex, "Failed to delete sequence");
                    await _dialogService.ShowErrorAsync("Error", $"Failed to delete sequence: {ex.Message}");
                })
                .DisposeWith(disposables);
        }
        
        /// <summary>
        /// Called when navigated to this view
        /// </summary>
        public void OnNavigatedTo(object parameter)
        {
            // Load data when navigated to this view
            LoadSequencesCommand.Execute().Subscribe(sequences => 
            {
                // If coming back from a detail view, try to reselect the previous sequence
                if (parameter is string sequenceId && !string.IsNullOrEmpty(sequenceId))
                {
                    SelectedSequence = Sequences.FirstOrDefault(s => s.Id == sequenceId);
                }
            });
        }
        
        /// <summary>
        /// Loads sequences from the service
        /// </summary>
        private async Task<IEnumerable<Sequence>> LoadSequencesAsync()
        {
            IsLoading = true;
            
            try
            {
                var sequences = await _sequenceService.GetAllSequencesAsync();
                
                // Update the source list
                _sequencesSource.Edit(list => 
                {
                    list.Clear();
                    list.AddRange(sequences);
                });
                
                return sequences;
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// Deletes the specified sequence
        /// </summary>
        private async Task<Unit> DeleteSequenceAsync(Sequence sequence)
        {
            if (sequence == null) return Unit.Default;
            
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Confirm Delete", 
                $"Are you sure you want to delete the sequence '{sequence.Name}'?");
                
            if (!confirmed) return Unit.Default;
            
            IsLoading = true;
            
            try
            {
                await _sequenceService.DeleteSequenceAsync(sequence.Id);
                
                // Remove from the source list
                _sequencesSource.Edit(list => list.Remove(sequence));
                
                // If this was the selected sequence, clear the selection
                if (SelectedSequence == sequence)
                {
                    SelectedSequence = null;
                }
                
                await _dialogService.ShowInformationAsync("Success", "Sequence deleted successfully");
                return Unit.Default;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
