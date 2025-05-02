using Instrument.Data.UI.ViewModels;
using System.Windows.Controls;

namespace Instrument.Data.UI.Views
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
