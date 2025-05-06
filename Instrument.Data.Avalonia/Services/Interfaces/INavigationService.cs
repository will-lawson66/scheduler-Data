using Instrument.Data.Avalonia.ViewModels.Base;
using ReactiveUI;
using System;

namespace Instrument.Data.Avalonia.Services
{
    /// <summary>
    /// Service for handling navigation between views and view models
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Initializes the navigation service with the content host
        /// </summary>
        /// <param name="owner">The owner of the navigation (typically the main window)</param>
        /// <param name="contentSetter">Action to set content in the content host</param>
        void Initialize(object owner, Action<object> contentSetter);
        
        /// <summary>
        /// Navigates to a view model
        /// </summary>
        /// <typeparam name="TViewModel">The type of the view model to navigate to</typeparam>
        /// <param name="parameter">Optional parameter to pass to the view model</param>
        void NavigateTo<TViewModel>(object parameter = null) where TViewModel : ViewModelBase;
        
        /// <summary>
        /// Navigates to a routable view model using ReactiveUI routing
        /// </summary>
        /// <typeparam name="TViewModel">The type of the view model to navigate to</typeparam>
        /// <param name="parameter">Optional parameter to pass to the view model</param>
        void NavigateToRoute<TViewModel>(object parameter = null) where TViewModel : class, IRoutableViewModel;
        
        /// <summary>
        /// Navigates back to the previous view
        /// </summary>
        void GoBack();
        
        /// <summary>
        /// Resets the navigation stack and navigates to the specified view model
        /// </summary>
        /// <typeparam name="TViewModel">The type of the view model to navigate to</typeparam>
        /// <param name="parameter">Optional parameter to pass to the view model</param>
        void NavigateAndReset<TViewModel>(object parameter = null) where TViewModel : ViewModelBase;
    }
    
    /// <summary>
    /// Interface for view models that need to be initialized with parameters
    /// </summary>
    public interface IInitializable
    {
        /// <summary>
        /// Initializes the view model with the given parameter
        /// </summary>
        /// <param name="parameter">The parameter to initialize with</param>
        void Initialize(object parameter);
    }
    
    /// <summary>
    /// Interface for view models that need to be notified when navigated to
    /// </summary>
    public interface INavigationAware
    {
        /// <summary>
        /// Called when the view model is navigated to
        /// </summary>
        /// <param name="parameter">The navigation parameter</param>
        void OnNavigatedTo(object parameter);
    }
}
