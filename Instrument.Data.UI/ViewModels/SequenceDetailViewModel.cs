using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Instrument\.Data.Entities;
using Instrument\.Data.Exceptions;
using Instrument\.Data.Services;
using Instrument.Scheduling.UI.Services;
using Instrument.Scheduling.UI.Views;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Instrument.Scheduling.UI.ViewModels
{
    public partial class SequenceDetailViewModel : EntityViewModelBase<Sequence>
    {
        private readonly SequenceService _sequenceService;
        private readonly ParameterService _parameterService;

        [ObservableProperty]
        private ObservableCollection<Parameter> _availableParameters = new();

        [ObservableProperty]
        private ObservableCollection<Parameter> _sequenceParameters = new();

        [ObservableProperty]
        private Parameter? _selectedAvailableParameter;

        [ObservableProperty]
        private Parameter? _selectedSequenceParameter;

        [ObservableProperty]
        private string _sequenceName = string.Empty;

        [ObservableProperty]
        private string _sequenceDescription = string.Empty;

        [ObservableProperty]
        private string _worstCaseTime = "00:00:30";

        [ObservableProperty]
        private bool _canBeParallel;

        public SequenceDetailViewModel(
            NavigationService navigationService,
            DialogService dialogService,
            ILogger<SequenceDetailViewModel> logger,
            SequenceService sequenceService,
            ParameterService parameterService)
            : base(navigationService, dialogService, logger)
        {
            _sequenceService = sequenceService;
            _parameterService = parameterService;
        }

        protected override void CreateNewEntity()
        {
            Title = "Create New Sequence";
            SequenceName = string.Empty;
            SequenceDescription = string.Empty;
            WorstCaseTime = "00:00:30";
            CanBeParallel = false;
            
            LoadAvailableParameters();
        }

        protected override void LoadEntity()
        {
            Title = "Edit Sequence";
            LoadSequenceAsync();
        }

        private async void LoadSequenceAsync()
        {
            try
            {
                IsLoading = true;
                
                var sequence = await _sequenceService.GetSequenceAsync(EntityId);
                if (sequence == null)
                {
                    DialogService.ShowError("Error", $"Sequence with ID {EntityId} not found.");
                    NavigationService.NavigateTo<SequencesView>();
                    return;
                }

                Entity = sequence;
                SequenceName = sequence.Name;
                SequenceDescription = sequence.Description ?? string.Empty;
                WorstCaseTime = sequence.WorstCaseTime.ToString(@"hh\:mm\:ss");
                CanBeParallel = sequence.CanBeParallel;
                
                await LoadSequenceParametersAsync();
                await LoadAvailableParameters();
            }
            catch (Exception ex)
            {
                DialogService.ShowError("Error", $"Failed to load sequence: {ex.Message}");
                Logger.LogError(ex, "Failed to load sequence {Id}", EntityId);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadSequenceParametersAsync()
        {
            try
            {
                var parameters = await _parameterService.GetParametersForSequenceAsync(EntityId);
                SequenceParameters.Clear();
                foreach (var parameter in parameters)
                {
                    SequenceParameters.Add(parameter);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load sequence parameters for {Id}", EntityId);
            }
        }

        private async Task LoadAvailableParameters()
        {
            try
            {
                var allParameters = await _parameterService.GetAllParametersAsync();
                AvailableParameters.Clear();
                
                // If we're editing, filter out parameters that are already in the sequence
                if (!IsNew && SequenceParameters.Any())
                {
                    var sequenceParameterIds = SequenceParameters.Select(p => p.Id).ToHashSet();
                    foreach (var parameter in allParameters.Where(p => !sequenceParameterIds.Contains(p.Id)))
                    {
                        AvailableParameters.Add(parameter);
                    }
                }
                else
                {
                    foreach (var parameter in allParameters)
                    {
                        AvailableParameters.Add(parameter);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load available parameters");
            }
        }

        [RelayCommand]
        private async Task AddParameterAsync()
        {
            if (SelectedAvailableParameter == null || IsNew)
                return;

            try
            {
                await _parameterService.AddParameterToSequenceAsync(
                    EntityId,
                    SelectedAvailableParameter.Id,
                    SequenceParameters.Count);
                
                SequenceParameters.Add(SelectedAvailableParameter);
                AvailableParameters.Remove(SelectedAvailableParameter);
                SelectedAvailableParameter = null;
            }
            catch (Exception ex)
            {
                DialogService.ShowError("Error", $"Failed to add parameter to sequence: {ex.Message}");
                Logger.LogError(ex, "Failed to add parameter to sequence");
            }
        }

        [RelayCommand]
        private async Task RemoveParameterAsync()
        {
            if (SelectedSequenceParameter == null || IsNew)
                return;

            try
            {
                await _parameterService.RemoveParameterFromSequenceAsync(
                    EntityId,
                    SelectedSequenceParameter.Id);
                
                AvailableParameters.Add(SelectedSequenceParameter);
                SequenceParameters.Remove(SelectedSequenceParameter);
                SelectedSequenceParameter = null;
            }
            catch (Exception ex)
            {
                DialogService.ShowError("Error", $"Failed to remove parameter from sequence: {ex.Message}");
                Logger.LogError(ex, "Failed to remove parameter from sequence");
            }
        }

        protected override async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(SequenceName))
            {
                DialogService.ShowWarning("Validation Error", "Sequence name is required.");
                return;
            }

            try
            {
                IsLoading = true;
                
                // Parse the worst case time
                if (!TimeSpan.TryParse(WorstCaseTime, out var worstCaseTime))
                {
                    DialogService.ShowWarning("Validation Error", "Invalid worst case time format. Use hh:mm:ss format.");
                    return;
                }

                var sequence = new Sequence
                {
                    Id = IsNew ? Guid.NewGuid().ToString() : EntityId,
                    Name = SequenceName,
                    Description = !string.IsNullOrWhiteSpace(SequenceDescription) ? SequenceDescription : null,
                    WorstCaseTime = worstCaseTime,
                    CanBeParallel = CanBeParallel
                };

                if (IsNew)
                {
                    await _sequenceService.CreateSequenceAsync(sequence);
                    DialogService.ShowInformation("Success", "Sequence created successfully.");
                }
                else
                {
                    await _sequenceService.UpdateSequenceAsync(sequence);
                    DialogService.ShowInformation("Success", "Sequence updated successfully.");
                }

                NavigationService.NavigateTo<SequencesView>();
            }
            catch (StorageProviderException ex)
            {
                DialogService.ShowError("Error", $"Failed to save sequence: {ex.Message}");
                Logger.LogError(ex, "Failed to save sequence");
            }
            catch (Exception ex)
            {
                DialogService.ShowError("Error", $"An unexpected error occurred: {ex.Message}");
                Logger.LogError(ex, "Unexpected error while saving sequence");
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected override async Task DeleteAsync()
        {
            if (IsNew)
                return;

            var confirmed = DialogService.ShowConfirmation(
                "Confirm Delete",
                $"Are you sure you want to delete the sequence '{SequenceName}'?");

            if (!confirmed)
                return;

            try
            {
                IsLoading = true;
                await _sequenceService.DeleteSequenceAsync(EntityId);
                DialogService.ShowInformation("Success", "Sequence deleted successfully.");
                NavigationService.NavigateTo<SequencesView>();
            }
            catch (Exception ex)
            {
                DialogService.ShowError("Error", $"Failed to delete sequence: {ex.Message}");
                Logger.LogError(ex, "Failed to delete sequence {Id}", EntityId);
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected override void GoBack()
        {
            NavigationService.NavigateTo<SequencesView>();
        }
    }
}
