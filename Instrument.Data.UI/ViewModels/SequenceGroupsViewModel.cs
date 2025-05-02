using Instrument.Scheduling.UI.Services;
using Microsoft.Extensions.Logging;

namespace Instrument.Scheduling.UI.ViewModels
{
    public class SequenceGroupsViewModel : ViewModelBase
    {
        public SequenceGroupsViewModel(NavigationService navigationService, DialogService dialogService, ILogger<SequenceGroupsViewModel> logger) 
            : base(navigationService, dialogService, logger)
        {
            Title = "Sequence Groups";
        }
    }
}
