using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Instrument.Data.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Splat;
using System;

namespace Instrument.Data.Avalonia
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Get the host's service provider
                var serviceProvider = (IServiceProvider)Locator.Current.GetService(typeof(IServiceProvider));
                
                // Register the ViewLocator
                AvaloniaLocator.CurrentMutable.Bind<IDataTemplate>().ToConstant(new ViewLocator(serviceProvider));
                
                // Register Splat's locator for ReactiveUI services
                Locator.CurrentMutable.RegisterConstant(serviceProvider, typeof(IServiceProvider));
                
                // Create main window with ViewModel from DI
                desktop.MainWindow = new MainWindow
                {
                    DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
