using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Instrument.Data.Avalonia.Views
{
    public partial class SequencesView : UserControl
    {
        public SequencesView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
