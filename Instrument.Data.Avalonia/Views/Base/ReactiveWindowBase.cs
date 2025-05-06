using Avalonia.ReactiveUI;
using Instrument.Data.Avalonia.ViewModels.Base;
using ReactiveUI;
using System;
using System.Reactive.Disposables;

namespace Instrument.Data.Avalonia.Views.Base
{
    /// <summary>
    /// Base class for all reactive windows in the application.
    /// Implements proper view activation and ViewModel binding patterns.
    /// </summary>
    /// <typeparam name="TViewModel">The type of the ViewModel.</typeparam>
    public class ReactiveWindowBase<TViewModel> : ReactiveWindow<TViewModel> 
        where TViewModel : ViewModelBase
    {
        protected ReactiveWindowBase()
        {
            // WhenActivated is called when the window is attached to the visual tree
            this.WhenActivated(disposables =>
            {
                // Handle window activation
                HandleActivation();
                
                // Register cleanup action for when the window is detached
                Disposable.Create(HandleDeactivation)
                    .DisposeWith(disposables);
                
                // Register any additional handlers
                SetupWindowActivationHandlers(disposables);
            });
        }
        
        /// <summary>
        /// Override this method to handle window activation.
        /// </summary>
        protected virtual void HandleActivation() { }
        
        /// <summary>
        /// Override this method to handle window deactivation.
        /// </summary>
        protected virtual void HandleDeactivation() { }
        
        /// <summary>
        /// Override this method to set up additional activation handlers.
        /// </summary>
        /// <param name="disposables">The CompositeDisposable for registering disposables.</param>
        protected virtual void SetupWindowActivationHandlers(CompositeDisposable disposables) { }
    }
}
