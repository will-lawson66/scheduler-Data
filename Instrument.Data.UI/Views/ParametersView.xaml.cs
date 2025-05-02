using Instrument.Scheduling.UI.ViewModels;
using System.Windows.Controls;

namespace Instrument.Scheduling.UI.Views
{
    public partial class ParametersView : UserControl
    {
        public ParametersView(ParametersViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
