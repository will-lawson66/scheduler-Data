using ReactiveUI;
using System;
using System.Reactive.Disposables;
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
        
        public ViewModelActivator Activator { get; } = new ViewModelActivator();
        
        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }
        
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
            this.WhenActivated(disposables =>
            {
                HandleActivation();
                
                Disposable
                    .Create(HandleDeactivation)
                    .DisposeWith(disposables);
            });
        }
        
        protected virtual void HandleActivation() { }
        
        protected virtual void HandleDeactivation() { }
        
        protected async Task ExecuteWithLoadingAsync(Func<Task> action)
        {
            try
            {
                IsLoading = true;
                await action();
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        protected void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }
        
        public void Dispose()
        {
            _disposables.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
