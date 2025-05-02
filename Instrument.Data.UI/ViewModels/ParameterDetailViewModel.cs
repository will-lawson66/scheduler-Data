using Instrument.Data.Entities;
using Instrument.Data.UI.Services;
using Microsoft.Extensions.Logging;

namespace Instrument.Data.UI.ViewModels
{
    public class ParameterDetailViewModel : EntityViewModelBase<Parameter>
    {
        public ParameterDetailViewModel(NavigationService navigationService, DialogService dialogService, ILogger<ParameterDetailViewModel> logger) 
            : base(navigationService, dialogService, logger)
        {
            Title = "Parameter Details";
        }

        protected override void CreateNewEntity()
        {
            IsNew = true;
            Title = "Create New Parameter";
        }

        protected override void LoadEntity()
        {
            IsNew = false;
            Title = "Edit Parameter";
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
