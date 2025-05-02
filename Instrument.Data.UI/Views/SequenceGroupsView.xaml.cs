using Instrument.Data.UI.ViewModels;
using System.Windows.Controls;

namespace Instrument.Data.UI.Views
{
    public partial class SequenceGroupsView : UserControl
    {
        public SequenceGroupsView(SequenceGroupsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
