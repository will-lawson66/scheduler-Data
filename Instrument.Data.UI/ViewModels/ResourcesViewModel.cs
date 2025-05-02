using Instrument.Data.UI.Services;
using Microsoft.Extensions.Logging;

namespace Instrument.Data.UI.ViewModels
{
    public class ResourcesViewModel : ViewModelBase
    {
        public ResourcesViewModel(NavigationService navigationService, DialogService dialogService, ILogger<ResourcesViewModel> logger) 
            : base(navigationService, dialogService, logger)
        {
            Title = "Resources";
        }
    }
}
