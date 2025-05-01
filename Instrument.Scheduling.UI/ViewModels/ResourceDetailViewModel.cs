using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.UI.Services;
using Microsoft.Extensions.Logging;

namespace Instrument.Scheduling.UI.ViewModels
{
    public class ResourceDetailViewModel : EntityViewModelBase<Resource>
    {
        public ResourceDetailViewModel(NavigationService navigationService, DialogService dialogService, ILogger<ResourceDetailViewModel> logger) 
            : base(navigationService, dialogService, logger)
        {
            Title = "Resource Details";
        }

        protected override void CreateNewEntity()
        {
            IsNew = true;
            Title = "Create New Resource";
        }

        protected override void LoadEntity()
        {
            IsNew = false;
            Title = "Edit Resource";
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
