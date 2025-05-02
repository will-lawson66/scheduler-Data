using Instrument.Data.UI.Services;
using Microsoft.Extensions.Logging;

namespace Instrument.Data.UI.ViewModels
{
    public class ParametersViewModel : ViewModelBase
    {
        public ParametersViewModel(NavigationService navigationService, DialogService dialogService, ILogger<ParametersViewModel> logger) 
            : base(navigationService, dialogService, logger)
        {
            Title = "Parameters";
        }
    }
}
