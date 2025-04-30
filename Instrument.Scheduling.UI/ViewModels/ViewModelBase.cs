using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Instrument.Scheduling.UI.Services;
using Microsoft.Extensions.Logging;

namespace Instrument.Scheduling.UI.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        protected readonly NavigationService NavigationService;
        protected readonly DialogService DialogService;
        protected readonly ILogger Logger;

        protected ViewModelBase(NavigationService navigationService, DialogService dialogService, ILogger logger)
        {
            NavigationService = navigationService;
            DialogService = dialogService;
            Logger = logger;
        }

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _title = string.Empty;

        [RelayCommand]
        protected virtual void GoBack()
        {
            // Override in derived classes if needed
        }
    }
    
    public abstract class EntityViewModelBase<T> : ViewModelBase, INavigationAware
    {
        [ObservableProperty]
        protected T? _entity;
        
        [ObservableProperty]
        protected string _entityId = string.Empty;
        
        [ObservableProperty]
        protected bool _isNew;

        protected EntityViewModelBase(
            NavigationService navigationService,
            DialogService dialogService,
            ILogger logger) : base(navigationService, dialogService, logger)
        {
        }

        public virtual void OnNavigatedTo(object parameter)
        {
            if (parameter is string id)
            {
                EntityId = id;
                IsNew = false;
                LoadEntity();
            }
            else
            {
                IsNew = true;
                CreateNewEntity();
            }
        }

        protected abstract void LoadEntity();
        protected abstract void CreateNewEntity();
        
        [RelayCommand]
        protected abstract Task SaveAsync();
        
        [RelayCommand]
        protected abstract Task DeleteAsync();
    }
}
