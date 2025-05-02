using Instrument.Data.UI.Services;
using Microsoft.Extensions.Logging;

namespace Instrument.Data.UI.ViewModels
{
    public class RangesViewModel : ViewModelBase
    {
        public RangesViewModel(NavigationService navigationService, DialogService dialogService, ILogger<RangesViewModel> logger) 
            : base(navigationService, dialogService, logger)
        {
            Title = "Ranges";
        }
    }
}
