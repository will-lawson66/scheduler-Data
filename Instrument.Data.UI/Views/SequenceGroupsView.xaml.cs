using Instrument.Scheduling.UI.ViewModels;
using System.Windows.Controls;

namespace Instrument.Scheduling.UI.Views
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
