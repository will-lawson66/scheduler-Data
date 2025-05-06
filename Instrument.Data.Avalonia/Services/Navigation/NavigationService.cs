using Avalonia.Controls;
using Instrument.Data.Avalonia.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Instrument.Data.Avalonia.Services.Navigation
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NavigationService> _logger;
        private readonly Stack<(Type ViewModel, object Parameter)> _navigationStack = new();
        
        private object _navigationOwner;
        private Action<object> _contentSetter;
        
        public NavigationService(
            IServiceProvider serviceProvider,
            ILogger<NavigationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        
        public void Initialize(object owner, Action<object> contentSetter)
        {
            _navigationOwner = owner;
            _contentSetter = contentSetter;
        }
        
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Navigation to {ViewModelType} failed", typeof(TViewModel).Name);
                throw;
            }
        }
        
        public void GoBack()
        {
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
    }
}
