using Instrument.Data.Avalonia.ViewModels.Base;
using System;

namespace Instrument.Data.Avalonia.Services
{
    public interface INavigationService
    {
        void Initialize(object owner, Action<object> contentSetter);
        void NavigateTo<TViewModel>(object parameter = null) where TViewModel : ViewModelBase;
        void GoBack();
    }
    
    public interface IInitializable
    {
        void Initialize(object parameter);
    }
    
    public interface INavigationAware
    {
        void OnNavigatedTo(object parameter);
    }
}
