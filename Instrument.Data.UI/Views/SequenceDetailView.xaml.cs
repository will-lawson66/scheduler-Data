using Instrument.Data.UI.ViewModels;
using System.Windows.Controls;

namespace Instrument.Data.UI.Views
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
