using Instrument.Scheduling.UI.ViewModels;
using System.Windows.Controls;

namespace Instrument.Scheduling.UI.Views
{
    public partial class SequenceDetailView : UserControl
    {
        public SequenceDetailView(SequenceDetailViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
