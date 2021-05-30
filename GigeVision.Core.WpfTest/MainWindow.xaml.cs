using GigeVision.Core.Models;
using System.Windows;

namespace GigeVision.Core.WpfTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Camera camera;

        private Gvcp gvcp;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += MainWindow_Loaded;
        }

        public byte[] RawBytes { get; set; }

        public int FrameCounter { get; set; }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            camera = new();
            camera.IP = "192.168.10.244";
            gvcp = new();
            camera.FrameReady += FrameReady;
            image.WidthImage = 640;
            image.HeightImage = 480;
        }

        private void FrameReady(object sender, byte[] e)
        {
            image.Dispatcher.Invoke(() => image.RawBytes = e);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await camera.StartStreamAsync("192.168.10.227").ConfigureAwait(false);
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            await camera.StopStream().ConfigureAwait(false);
        }
    }
}