using Instrument.Data.UI.ViewModels;
using System.Windows.Controls;

namespace Instrument.Data.UI.Views
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
