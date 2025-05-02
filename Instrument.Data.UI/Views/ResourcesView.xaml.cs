using Instrument.Scheduling.UI.ViewModels;
using System.Windows.Controls;

namespace Instrument.Scheduling.UI.Views
{
    public partial class ResourcesView : UserControl
    {
        public ResourcesView(ResourcesViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
