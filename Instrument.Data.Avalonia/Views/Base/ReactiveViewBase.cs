using Avalonia.ReactiveUI;
using Instrument.Data.Avalonia.ViewModels.Base;
using ReactiveUI;
using System;
using System.Reactive.Disposables;

namespace Instrument.Data.Avalonia.Views.Base
{
    /// <summary>
    /// Base class for all reactive user controls in the application.
    /// Implements proper view activation and ViewModel binding patterns.
    /// </summary>
    /// <typeparam name="TViewModel">The type of the ViewModel.</typeparam>
    public class ReactiveViewBase<TViewModel> : ReactiveUserControl<TViewModel> 
        where TViewModel : ViewModelBase
    {
        protected ReactiveViewBase()
        {
            // WhenActivated is called when the view is attached to the visual tree
            this.WhenActivated(disposables =>
            {
                // Handle view activation
                HandleActivation();
                
                // Register cleanup action for when the view is detached
                Disposable.Create(HandleDeactivation)
                    .DisposeWith(disposables);
                
                // Register any additional handlers
                SetupViewActivationHandlers(disposables);
            });
        }
        
        /// <summary>
        /// Override this method to handle view activation.
        /// </summary>
        protected virtual void HandleActivation() { }
        
        /// <summary>
        /// Override this method to handle view deactivation.
        /// </summary>
        protected virtual void HandleDeactivation() { }
        
        /// <summary>
        /// Override this method to set up additional activation handlers.
        /// </summary>
        /// <param name="disposables">The CompositeDisposable for registering disposables.</param>
        protected virtual void SetupViewActivationHandlers(CompositeDisposable disposables) { }
    }
}
