using GigeVision.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GigeVisionLibrary.Test.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PixelFormat pixelFormat = PixelFormats.Gray8;

        private Gvcp gvcp;

        private BitmapSource image;
        private int fpsCount;

        private int width = 1024;

        private int height = 728;

        private int bytesPerPixel;
        private Camera camera;

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

        public BitmapSource Image
        {
            get => image;
            set => image = value;
        }

        private async void Setup()
        {
            camera = new Camera();
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
            Dispatcher.Invoke(() => lightControl.ImagePtr = (IntPtr)sender, System.Windows.Threading.DispatcherPriority.Render);
            fpsCount++;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (camera.IsStreaming)
            {
                await camera.StopStream().ConfigureAwait(false);
            }
            else
            {
                // await camera.SetResolutionAsync(800, 600).ConfigureAwait(false);

                width = (int)camera.Width;
                height = (int)camera.Height;
                Dispatcher.Invoke(() =>
                {
                    lightControl.WidthImage = width;
                    lightControl.HeightImage = height;
                });
                await camera.StartStreamAsync().ConfigureAwait(false);
            }
        }
    }
}