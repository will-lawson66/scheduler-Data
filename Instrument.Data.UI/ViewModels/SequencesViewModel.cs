using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Instrument.Data.Entities;
using Instrument.Data.Exceptions;
using Instrument.Data.Interfaces;
using Instrument.Data.Services;
using Instrument.Data.UI.Services;
using Instrument.Data.UI.Views;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Instrument.Data.UI.ViewModels
{
    public partial class SequencesViewModel : ViewModelBase, INavigationAware
    {
        private readonly SequenceService _sequenceService;

        [ObservableProperty]
        private ObservableCollection<Sequence> _sequences = new();

        [ObservableProperty]
        private Sequence? _selectedSequence;

        public bool HasNoSequences => !Sequences.Any();

        public SequencesViewModel(
            NavigationService navigationService,
            DialogService dialogService,
            ILogger<SequencesViewModel> logger,
            SequenceService sequenceService) 
            : base(navigationService, dialogService, logger)
        {
            Title = "Sequences";
            _sequenceService = sequenceService;
        }

        public void OnNavigatedTo(object parameter)
        {
            // Load sequences when navigating to this view
            LoadSequencesAsync().ConfigureAwait(false);
        }

        [RelayCommand]
        private async Task LoadSequencesAsync()
        {
            try
            {
                IsLoading = true;
                var sequences = await _sequenceService.GetAllSequencesAsync();
                Sequences.Clear();
                foreach (var sequence in sequences)
                {
                    Sequences.Add(sequence);
                }
                OnPropertyChanged(nameof(HasNoSequences));
            }
            catch (StorageProviderException ex)
            {
                DialogService.ShowError("Error", $"Failed to load sequences: {ex.Message}");
                Logger.LogError(ex, "Failed to load sequences");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ViewSequence()
        {
            if (SelectedSequence is null)
                return;

            NavigationService.NavigateTo<SequenceDetailView>(SelectedSequence.Id);
        }

        [RelayCommand]
        private void CreateSequence()
        {
            NavigationService.NavigateTo<SequenceDetailView>("new");
        }

        [RelayCommand]
        private async Task DeleteSequenceAsync()
        {
            if (SelectedSequence is null)
                return;

            var confirmed = DialogService.ShowConfirmation(
                "Confirm Delete",
                $"Are you sure you want to delete the sequence '{SelectedSequence.Name}'?"
            );

            if (!confirmed)
                return;

            try
            {
                await _sequenceService.DeleteSequenceAsync(SelectedSequence.Id);
                Sequences.Remove(SelectedSequence);
                SelectedSequence = null;
                OnPropertyChanged(nameof(HasNoSequences));
                
                DialogService.ShowInformation("Success", "Sequence deleted successfully.");
            }
            catch (Exception ex)
            {
                DialogService.ShowError("Error", $"Failed to delete sequence: {ex.Message}");
                Logger.LogError(ex, "Failed to delete sequence {Id}", SelectedSequence.Id);
            }
        }
    }
}
