using Instrument\.Data.Entities;
using Instrument.Scheduling.UI.Services;
using Microsoft.Extensions.Logging;

namespace Instrument.Scheduling.UI.ViewModels
{
    public class RangeDetailViewModel : EntityViewModelBase<Entities.Range>
    {
        public RangeDetailViewModel(NavigationService navigationService, DialogService dialogService, ILogger<RangeDetailViewModel> logger) 
            : base(navigationService, dialogService, logger)
        {
            Title = "Range Details";
        }

        protected override void CreateNewEntity()
        {
            IsNew = true;
            Title = "Create New Range";
        }

        protected override void LoadEntity()
        {
            IsNew = false;
            Title = "Edit Range";
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
