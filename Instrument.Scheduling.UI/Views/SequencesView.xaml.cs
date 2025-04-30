using Instrument.Scheduling.UI.ViewModels;
using System.Windows.Controls;

namespace Instrument.Scheduling.UI.Views
{
    public partial class SequencesView : UserControl
    {
        public SequencesView(SequencesViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
            // Load sequences when view is loaded
            Loaded += (s, e) => viewModel.LoadSequencesCommand.Execute(null);
        }
    }
}
