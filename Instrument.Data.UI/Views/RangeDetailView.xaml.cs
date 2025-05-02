using Instrument.Scheduling.UI.ViewModels;
using System.Windows.Controls;

namespace Instrument.Scheduling.UI.Views
{
    public partial class RangeDetailView : UserControl
    {
        public RangeDetailView(RangeDetailViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
