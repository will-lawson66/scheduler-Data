using Instrument.Data.UI.ViewModels;
using System.Windows.Controls;

namespace Instrument.Data.UI.Views
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
