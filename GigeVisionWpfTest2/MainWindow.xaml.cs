using GigeVision.Core.Models;
using System.Timers;
using System.Windows;

namespace GigeVisionWpfTest2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Camera camera;
        private readonly Timer timer;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            camera = new();
            camera.IP = "192.168.10.244";
            camera.FrameReady += FrameReady;
            image.WidthImage = 1032;
            image.HeightImage = 1032;
            timer = new();
            timer.Elapsed += Timer_Elapsed;
            timer.Interval = 1000;
        }

        public byte[] RawBytes { get; set; }

        public int FrameCounter { get; set; }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
            timer.Start();
            fps.Dispatcher.Invoke(() =>
            {
                fps.Text = $"FPS: {FrameCounter}";
            });
            FrameCounter = 0;
        }

        private void FrameReady(object sender, byte[] e)
        {
            image.Dispatcher.Invoke(() => image.RawBytes = e);
            FrameCounter++;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();
            await camera.StartStreamAsync("192.168.10.172").ConfigureAwait(false);
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            await camera.StopStream().ConfigureAwait(false);
            timer.Stop();
            FrameCounter = 0;
            fps.Dispatcher.Invoke(() => fps.Text = $"FPS: {FrameCounter}");
        }
    }
}