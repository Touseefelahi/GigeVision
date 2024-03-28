using GigeVision.Core.Services;
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
        private Camera camera;
        private int fpsCount;
        private int height = 600;
        private bool isLoaded;
        private int width = 800;

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

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!Camera.Gvcp.IsXmlFileLoaded)
            {
                isLoaded = await Camera.Gvcp.ReadXmlFileAsync();
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

        private void FrameReady(object sender, byte[] e)
        {
            Dispatcher.Invoke(() => lightControl.RawBytes = e, System.Windows.Threading.DispatcherPriority.Render);
            fpsCount++;
        }

        private async void Setup()
        {
            camera = new Camera();
            camera.StreamReceiver = new CameraStreamDisplay();
            GigeVision.Core.NetworkService.AllowAppThroughFirewall();
            var listOfDevices = await camera.Gvcp.GetAllGigeDevicesInNetworkAsnyc().ConfigureAwait(true);
            cameraCount.Text = "Cam count: " + listOfDevices.Count.ToString();
            if (listOfDevices.Count > 0)
            {
                Camera.IP = listOfDevices.FirstOrDefault()?.IP;
                Camera.RxIP = listOfDevices.FirstOrDefault()?.NetworkIP;
            }
            //camera.Gvcp.ForceIPAsync(listOfDevices[0].MacAddress, "192.168.10.243");
            camera.Payload = 5000;

            camera.IsMulticast = false;
            camera.FrameReady += FrameReady;
            camera.Gvcp.ElapsedOneSecond += UpdateFps;
        }

        private void UpdateFps(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            Fps.Text = fpsCount.ToString(), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            fpsCount = 0;
        }
    }
}