using Instrument.Data.Entities;
using Instrument.Data.UI.Services;
using Microsoft.Extensions.Logging;

namespace Instrument.Data.UI.ViewModels
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
