using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Instrument.Data.UI.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Instrument.Data.UI.Services.Navigation
{
    /// <summary>
    /// Implements navigation between views in a WPF application.
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Frame _navigationFrame;
        private readonly Dictionary<Type, Type> _viewModelToViewMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationService"/> class.
        /// </summary>
        /// <param name="serviceProvider">The dependency injection service provider.</param>
        /// <param name="navigationFrame">The frame used for navigation.</param>
        public NavigationService(IServiceProvider serviceProvider, Frame navigationFrame)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _navigationFrame = navigationFrame ?? throw new ArgumentNullException(nameof(navigationFrame));
            _viewModelToViewMap = new Dictionary<Type, Type>();
        }

        /// <summary>
        /// Registers a mapping between a ViewModel type and a View type.
        /// </summary>
        /// <typeparam name="TViewModel">The type of the ViewModel.</typeparam>
        /// <typeparam name="TView">The type of the View.</typeparam>
        public void Register<TViewModel, TView>()
            where TViewModel : class
            where TView : Page
        {
            _viewModelToViewMap[typeof(TViewModel)] = typeof(TView);
        }

        /// <inheritdoc/>
        public void NavigateTo<TViewModel>(object parameter = null)
        {
            var viewModelType = typeof(TViewModel);
            if (!_viewModelToViewMap.ContainsKey(viewModelType))
            {
                throw new InvalidOperationException($"No view is registered for {viewModelType.FullName}");
            }

            var viewType = _viewModelToViewMap[viewModelType];
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
            var view = (Page)Activator.CreateInstance(viewType);
            view.DataContext = viewModel;

            // If the ViewModel implements an initialization interface, call it
            if (viewModel is IInitializable initializable)
            {
                initializable.Initialize(parameter);
            }

            _navigationFrame.Navigate(view);
        }

        /// <inheritdoc/>
        public bool GoBack()
        {
            if (_navigationFrame.CanGoBack)
            {
                _navigationFrame.GoBack();
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public async Task<object> ShowDialogAsync<TViewModel>(object parameter = null)
        {
            var viewModelType = typeof(TViewModel);
            if (!_viewModelToViewMap.ContainsKey(viewModelType))
            {
                throw new InvalidOperationException($"No view is registered for {viewModelType.FullName}");
            }

            var viewType = _viewModelToViewMap[viewModelType];
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
            var window = (Window)Activator.CreateInstance(viewType);
            window.DataContext = viewModel;

            // If the ViewModel implements an initialization interface, call it
            if (viewModel is IInitializable initializable)
            {
                initializable.Initialize(parameter);
            }

            var taskCompletionSource = new TaskCompletionSource<object>();

            window.Closed += (s, e) =>
            {
                if (window.DataContext is IDialogResult dialogResult)
                {
                    taskCompletionSource.SetResult(dialogResult.Result);
                }
                else
                {
                    taskCompletionSource.SetResult(null);
                }
            };

            window.ShowDialog();
            return await taskCompletionSource.Task;
        }
    }

    /// <summary>
    /// Interface for initializable ViewModels.
    /// </summary>
    public interface IInitializable
    {
        /// <summary>
        /// Initializes the ViewModel with the specified parameter.
        /// </summary>
        /// <param name="parameter">The parameter to initialize with.</param>
        void Initialize(object parameter);
    }

    /// <summary>
    /// Interface for ViewModels that provide a dialog result.
    /// </summary>
    public interface IDialogResult
    {
        /// <summary>
        /// Gets the dialog result.
        /// </summary>
        object Result { get; }
    }
}
