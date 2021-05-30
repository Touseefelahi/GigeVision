using GigeVision.Core.Models;
using System.Windows;

namespace GigeVision.Core.WpfTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Camera camera;

        private readonly Gvcp gvcp;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            camera = new();
            camera.IP = "192.168.10.197";
            gvcp = new();
            camera.FrameReady += FrameReady;
            image.WidthImage = 640;
            image.HeightImage = 480;
        }

        public byte[] RawBytes { get; set; }

        public int FrameCounter { get; set; }

        private void FrameReady(object sender, byte[] e)
        {
            image.Dispatcher.Invoke(() => image.RawBytes = e);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await camera.StartStreamAsync("192.168.10.172").ConfigureAwait(false);
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            await camera.StopStream().ConfigureAwait(false);
        }
    }
}