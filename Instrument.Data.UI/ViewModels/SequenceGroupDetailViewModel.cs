using Instrument.Data.Entities;
using Instrument.Data.UI.Services;
using Microsoft.Extensions.Logging;

namespace Instrument.Data.UI.ViewModels
{
    public class SequenceGroupDetailViewModel : EntityViewModelBase<SequenceGroup>
    {
        public SequenceGroupDetailViewModel(NavigationService navigationService, DialogService dialogService, ILogger<SequenceGroupDetailViewModel> logger) 
            : base(navigationService, dialogService, logger)
        {
            Title = "Sequence Group Details";
        }

        protected override void CreateNewEntity()
        {
            IsNew = true;
            Title = "Create New Sequence Group";
        }

        protected override void LoadEntity()
        {
            IsNew = false;
            Title = "Edit Sequence Group";
        }

        protected override Task SaveAsync()
        {
            return Task.CompletedTask;
        }

        protected override Task DeleteAsync()
        {
            return Task.CompletedTask;
        }
    }
}
