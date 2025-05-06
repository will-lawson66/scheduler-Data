using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Instrument.Data.Avalonia.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;

namespace Instrument.Data.Avalonia
{
    /// <summary>
    /// Maps ViewModels to Views using a naming convention and dependency injection.
    /// Used by ReactiveUI's routing system.
    /// </summary>
    public class ViewLocator : IDataTemplate
    {
        private readonly IServiceProvider _serviceProvider;

        public ViewLocator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Control Build(object data)
        {
            if (data is null)
                return new TextBlock { Text = "No data" };

            var viewModelType = data.GetType();
            var viewTypeName = viewModelType.FullName.Replace("ViewModel", "View");
            var viewType = Type.GetType(viewTypeName);

            if (viewType != null)
            {
                try
                {
                    // Try to resolve the view from the dependency injection container
                    var view = _serviceProvider.GetService(viewType) as Control;
                    
                    // If not registered in DI, create a new instance
                    if (view == null)
                    {
                        view = (Control)Activator.CreateInstance(viewType);
                    }

                    // If it's an IViewFor<>, set the ViewModel property
                    if (view is IViewFor viewFor && viewModelType.IsAssignableFrom(viewFor.GetType().GetGenericArguments()[0]))
                    {
                        viewFor.ViewModel = data;
                    }
                    else
                    {
                        // Otherwise set the DataContext
                        view.DataContext = data;
                    }

                    return view;
                }
                catch
                {
                    return new TextBlock { Text = $"Error creating view for {viewModelType.Name}" };
                }
            }

            return new TextBlock { Text = $"No view found for {viewModelType.Name}" };
        }

        public bool Match(object data)
        {
            return data is ViewModelBase;
        }
    }
}
