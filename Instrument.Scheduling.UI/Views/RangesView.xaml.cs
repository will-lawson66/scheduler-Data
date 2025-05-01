using Instrument.Scheduling.UI.ViewModels;
using System.Windows.Controls;

namespace Instrument.Scheduling.UI.Views
{
    public partial class RangesView : UserControl
    {
        public RangesView(RangesViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
