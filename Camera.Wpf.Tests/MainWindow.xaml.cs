using Camera.Wpf.Interfaces;
using Camera.Wpf.Models;
using System.Timers;
using System.Windows;

namespace Camera.Wpf.Tests
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ICamera camera;
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            Setup();
        }

        public ICamera Camera
        {
            get { return camera; }
            set { camera = value; }
        }

        private async void Setup()
        {
            camera = new GigeDaylightCamera("192.168.10.175");
            camera.OnFrameRecieved += FrameReady;
            var fpsDisplayTimer = new Timer(1000);
            fpsDisplayTimer.Elapsed += FpsDisplay;
            fpsDisplayTimer.Start();
        }

        private void FpsDisplay(object? sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() => Fps.Text = Camera.Fps.ToString(), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void FrameReady(object sender, byte[] e)
        {
            Dispatcher.Invoke(() => lightControl.RawBytes = e, System.Windows.Threading.DispatcherPriority.Render);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (camera.IsStreaming)
            {
                await camera.StopStreamAsync().ConfigureAwait(false);
            }
            else
            {
                await camera.StartStreamAsync().ConfigureAwait(false);
                Dispatcher.Invoke(() =>
                {
                    lightControl.WidthImage = camera.Resoluation.X;
                    lightControl.HeightImage = camera.Resoluation.Y;
                });
            }
        }
    }
}
