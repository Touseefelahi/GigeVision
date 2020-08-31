using GigeVision.Core.Models;
using Stira.WpfCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GigeVisionLibraryNuget.Text.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private int fpsCount;
        private int width = 800;

        private int height = 600;

        private Camera camera;

        private byte[] rawBytes;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            Setup();
        }

        public event PropertyChangedEventHandler PropertyChanged = (sender, e) => { };

        public Camera Camera
        {
            get { return camera; }
            set { camera = value; }
        }

        public byte[] RawBytes
        {
            get { return rawBytes; }
            set { rawBytes = value; }
        }

        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private async void Setup()
        {
            camera = new Camera
            {
                IsRawFrame = true,
                Payload = 8800
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
            //RawBytes = e;
            OnPropertyChanged(nameof(RawBytes));
            Dispatcher.Invoke(() => lightControl.RawBytes = e, System.Windows.Threading.DispatcherPriority.Render);
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
                width = (int)camera.Width;
                height = (int)camera.Height;
                Dispatcher.Invoke(() =>
                {
                    lightControl.WidthImage = width;
                    lightControl.HeightImage = height;
                    lightControl.IsColored = !camera.IsRawFrame;
                });
                await camera.StartStreamAsync().ConfigureAwait(false);
            }
        }
    }
}