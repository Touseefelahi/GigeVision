using GigeVision.Core.Models;
using System;
using System.Linq;
using System.Windows;

namespace GigeVisionLibrary.Test.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int fpsCount;
        private int width = 800;

        private int height = 600;

        private Camera camera;
        private bool isLoaded;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            Setup();
        }

        public Camera Camera
        {
            get { return camera; }
            set { camera = value; }
        }

        private async void Setup()
        {
            camera = new Camera
            {
                IsRawFrame = false,
                IsMulticast = true,
                MulticastIP = "239.168.10.15"
            };
            var listOfDevices = await camera.Gvcp.GetAllGigeDevicesInNetworkAsnyc().ConfigureAwait(false);
            if (listOfDevices.Count > 0) { Camera.IP = listOfDevices.FirstOrDefault()?.IP; }
            camera.FrameReady += FrameReady;
            camera.Gvcp.ElapsedOneSecond += UpdateFps;
        }

        private void UpdateFps(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            Fps.Text = fpsCount.ToString(), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            fpsCount = 0;
        }

        private void FrameReady(object sender, byte[] e)
        {
            Dispatcher.Invoke(() => lightControl.RawBytes = e, System.Windows.Threading.DispatcherPriority.Render);
            fpsCount++;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!isLoaded)
            {
                await camera.Gvcp.ReadAllRegisterAddressFromCameraAsync();
                if (camera.Gvcp.RegistersDictionary == null)
                {
                    return;
                }
                if (camera.Gvcp.RegistersDictionary.Count < 1)
                {
                    return;
                }

                isLoaded = true;
            }

            if (camera.IsStreaming)
            {
                await camera.StopStream().ConfigureAwait(false);
            }
            else
            {
                    width = (int)camera.Width;
                    height = (int)camera.Height;
                Dispatcher.Invoke(() =>
                {
                    lightControl.WidthImage = width;
                    lightControl.HeightImage = height;
                    lightControl.IsColored = !camera.IsRawFrame;
                });

                await camera.StartStreamAsync().ConfigureAwait(false);

                //camera.OffsetX = 264;
                //camera.OffsetY = 208;

                //await camera.SetOffsetAsync();

            }
        }
    }
}