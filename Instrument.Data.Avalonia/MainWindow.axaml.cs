using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Instrument.Data.Avalonia.ViewModels;

namespace Instrument.Data.Avalonia
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
