using Avalonia.Controls;
using Instrument.Data.Avalonia.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;

namespace Instrument.Data.Avalonia.Services.Navigation
{
    /// <summary>
    /// Implementation of a navigation service that combines direct content setting
    /// with ReactiveUI routing capabilities
    /// </summary>
    public class NavigationService : ReactiveObject, INavigationService, IScreen
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NavigationService> _logger;
        private readonly Stack<(Type ViewModel, object Parameter)> _navigationStack = new();
        
        private object _navigationOwner;
        private Action<object> _contentSetter;
        
        // ReactiveUI routing state
        public RoutingState Router { get; }
        
        public NavigationService(
            IServiceProvider serviceProvider,
            ILogger<NavigationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            // Initialize the ReactiveUI router
            Router = new RoutingState();
            
            // Register with Splat for ReactiveUI services
            Locator.CurrentMutable.RegisterConstant(this, typeof(IScreen));
        }
        
        public void Initialize(object owner, Action<object> contentSetter)
        {
            _navigationOwner = owner;
            _contentSetter = contentSetter;
        }
        
        /// <summary>
        /// Navigates to a view model by replacing the content in the content host
        /// </summary>
        public void NavigateTo<TViewModel>(object parameter = null) where TViewModel : ViewModelBase
        {
            try
            {
                // Add current view to navigation stack if there is one
                if (_contentSetter != null && _navigationOwner is ViewModelBase currentViewModel)
                {
                    var currentViewModelType = currentViewModel.GetType();
                    _navigationStack.Push((currentViewModelType, null)); // Save current state
                }
                
                // Resolve the ViewModel from DI
                var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
                
                // Initialize ViewModel with parameter if it implements IInitializable
                if (parameter != null && viewModel is IInitializable initializable)
                {
                    initializable.Initialize(parameter);
                }
                
                // Notify ViewModel about navigation if it implements INavigationAware
                if (viewModel is INavigationAware navigationAware)
                {
                    navigationAware.OnNavigatedTo(parameter);
                }
                
                // Update the content
                _contentSetter?.Invoke(viewModel);
                
                // If view model is routable, also update the routing state
                if (viewModel is IRoutableViewModel routableViewModel)
                {
                    Router.Navigate.Execute(routableViewModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Navigation to {ViewModelType} failed", typeof(TViewModel).Name);
                throw;
            }
        }
        
        /// <summary>
        /// Navigate to a routable view model using ReactiveUI routing
        /// </summary>
        public void NavigateToRoute<TViewModel>(object parameter = null) 
            where TViewModel : class, IRoutableViewModel
        {
            try
            {
                // Resolve the ViewModel from DI
                var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
                
                // Initialize ViewModel with parameter if it implements IInitializable
                if (parameter != null && viewModel is IInitializable initializable)
                {
                    initializable.Initialize(parameter);
                }
                
                // Navigate using ReactiveUI router
                Router.Navigate.Execute(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Navigation to route {ViewModelType} failed", typeof(TViewModel).Name);
                throw;
            }
        }
        
        /// <summary>
        /// Navigate back to the previous view model
        /// </summary>
        public void GoBack()
        {
            // Check if we can navigate back using ReactiveUI routing first
            if (Router.NavigateBack.CanExecute.FirstAsync().Wait())
            {
                Router.NavigateBack.Execute().Subscribe();
                return;
            }
            
            // Otherwise use our custom navigation stack
            if (_navigationStack.Count > 0)
            {
                var (viewModelType, parameter) = _navigationStack.Pop();
                
                // Use reflection to call NavigateTo with the proper generic type
                var navigateToMethod = typeof(NavigationService)
                    .GetMethod(nameof(NavigateTo))
                    .MakeGenericMethod(viewModelType);
                
                navigateToMethod.Invoke(this, new[] { parameter });
            }
        }
        
        /// <summary>
        /// Resets the navigation stack and navigates to the specified view model
        /// </summary>
        public void NavigateAndReset<TViewModel>(object parameter = null) 
            where TViewModel : ViewModelBase
        {
            // Clear the navigation stack
            _navigationStack.Clear();
            
            // Reset the ReactiveUI router
            Router.NavigationStack.Clear();
            
            // Navigate to the specified view model
            NavigateTo<TViewModel>(parameter);
        }
    }
}
