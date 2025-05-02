using Instrument.Data.UI.ViewModels;
using System.Windows.Controls;

namespace Instrument.Data.UI.Views
{
    public partial class RelationshipVisualizerView : UserControl
    {
        public RelationshipVisualizerView(RelationshipVisualizerViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
