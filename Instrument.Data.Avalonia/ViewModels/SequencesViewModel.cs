using Instrument.Data.Avalonia.Services;
using Instrument.Data.Avalonia.ViewModels.Base;
using Instrument.Data.Entities;
using Instrument.Data.Services;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Instrument.Data.Avalonia.ViewModels
{
    public class SequencesViewModel : ViewModelBase, INavigationAware
    {
        private readonly SequenceService _sequenceService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<SequencesViewModel> _logger;
        
        private ObservableCollection<Sequence> _sequences = new();
        private Sequence _selectedSequence;
        
        public ObservableCollection<Sequence> Sequences
        {
            get => _sequences;
            set => this.RaiseAndSetIfChanged(ref _sequences, value);
        }
        
        public Sequence SelectedSequence
        {
            get => _selectedSequence;
            set => this.RaiseAndSetIfChanged(ref _selectedSequence, value);
        }
        
        public bool HasNoSequences => !Sequences.Any();
        
        public ReactiveCommand<Unit, Unit> LoadSequencesCommand { get; }
        public ReactiveCommand<Unit, Unit> CreateSequenceCommand { get; }
        public ReactiveCommand<Unit, Unit> ViewSequenceCommand { get; }
        public ReactiveCommand<Unit, Unit> DeleteSequenceCommand { get; }
        
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
            
            LoadSequencesCommand = ReactiveCommand.CreateFromTask(LoadSequencesAsync);
            
            CreateSequenceCommand = ReactiveCommand.Create(() => 
                _navigationService.NavigateTo<SequenceDetailViewModel>("new"));
            
            ViewSequenceCommand = ReactiveCommand.Create(
                () => _navigationService.NavigateTo<SequenceDetailViewModel>(SelectedSequence?.Id),
                this.WhenAnyValue(x => x.SelectedSequence).Select(x => x != null));
            
            DeleteSequenceCommand = ReactiveCommand.CreateFromTask(
                DeleteSequenceAsync,
                this.WhenAnyValue(x => x.SelectedSequence).Select(x => x != null));
            
            // Observable for property changes
            this.WhenAnyValue(x => x.Sequences.Count)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(HasNoSequences)));
        }
        
        public void OnNavigatedTo(object parameter)
        {
            // Load data when navigated to this view
            LoadSequencesCommand.Execute().Subscribe();
        }
        
        private async Task LoadSequencesAsync()
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                try
                {
                    var sequences = await _sequenceService.GetAllSequencesAsync();
                    
                    Sequences.Clear();
                    foreach (var sequence in sequences)
                    {
                        Sequences.Add(sequence);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load sequences");
                    await _dialogService.ShowErrorAsync("Error", $"Failed to load sequences: {ex.Message}");
                }
            });
        }
        
        private async Task DeleteSequenceAsync()
        {
            if (SelectedSequence == null) return;
            
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Confirm Delete", 
                $"Are you sure you want to delete the sequence '{SelectedSequence.Name}'?");
                
            if (!confirmed) return;
            
            await ExecuteWithLoadingAsync(async () =>
            {
                try
                {
                    await _sequenceService.DeleteSequenceAsync(SelectedSequence.Id);
                    Sequences.Remove(SelectedSequence);
                    SelectedSequence = null;
                    
                    await _dialogService.ShowInformationAsync("Success", "Sequence deleted successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete sequence {Id}", SelectedSequence.Id);
                    await _dialogService.ShowErrorAsync("Error", $"Failed to delete sequence: {ex.Message}");
                }
            });
        }
    }
}
