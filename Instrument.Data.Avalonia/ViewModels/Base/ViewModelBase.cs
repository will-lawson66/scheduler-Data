using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Instrument.Data.Avalonia.ViewModels.Base
{
    public abstract class ViewModelBase : ReactiveObject, IActivatableViewModel, IDisposable
    {
        private bool _isLoading;
        private string _title = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _hasError;
        private readonly CompositeDisposable _disposables = new();
        
        // ViewModelActivator is used by ReactiveUI to know when a ViewModel is attached or detached
        public ViewModelActivator Activator { get; } = new ViewModelActivator();
        
        // IsBusy is a common pattern that prevents multiple operations running simultaneously
        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }
        
        // Observable indicating whether the ViewModel is currently busy
        // This can be used by commands to determine if they can execute
        public IObservable<bool> IsNotLoading => this.WhenAnyValue(x => x.IsLoading).Select(x => !x);
        
        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }
        
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                this.RaiseAndSetIfChanged(ref _errorMessage, value);
                HasError = !string.IsNullOrEmpty(value);
            }
        }
        
        public bool HasError
        {
            get => _hasError;
            set => this.RaiseAndSetIfChanged(ref _hasError, value);
        }
        
        protected ViewModelBase()
        {
            // WhenActivated is called when the associated view is attached to the visual tree
            this.WhenActivated(disposables =>
            {
                // Call virtual method for derived classes to override
                HandleActivation();
                
                // Register cleanup action when the view is detached
                Disposable
                    .Create(HandleDeactivation)
                    .DisposeWith(disposables);
                    
                // Register anything else that should be cleaned up when the view is detached
                SetupActivationHandlers(disposables);
            });
        }
        
        // Override this method in derived classes to handle activation logic
        protected virtual void HandleActivation() { }
        
        // Override this method in derived classes to handle deactivation logic
        protected virtual void HandleDeactivation() { }
        
        // Override this method to add disposables that should be cleaned up when VM is deactivated
        protected virtual void SetupActivationHandlers(CompositeDisposable disposables) { }
        
        // Helper method to handle loading state for async operations
        protected async Task ExecuteWithLoadingAsync(Func<Task> action)
        {
            try
            {
                IsLoading = true;
                await action();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        // Helper method to clear error state
        protected void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }
        
        // Implement IDisposable to clean up resources
        public virtual void Dispose()
        {
            _disposables.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
