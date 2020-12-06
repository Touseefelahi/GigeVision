using System.Windows;
using System.Windows.Markup;

namespace GigeVision.Wpf.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    [ContentPropertyAttribute("Items")]
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}