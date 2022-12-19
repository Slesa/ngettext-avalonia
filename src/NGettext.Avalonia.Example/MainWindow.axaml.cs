using Avalonia;
using Avalonia.Controls;

namespace NGettext.Avalonia.Example
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }
    }
}
