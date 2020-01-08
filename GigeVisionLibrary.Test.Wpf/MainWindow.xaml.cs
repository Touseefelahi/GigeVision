using GigeVision.Core.Models;
using System;
using System.Collections.Generic;
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
        private Gvsp gvsp;
        private Gvcp gvcp;

        public MainWindow()
        {
            InitializeComponent();
            Setup();
        }

        private async void Setup()
        {
            gvcp = new Gvcp() { };
            var devices = await gvcp.GetAllGigeDevicesInNetworkAsnyc();
            if (devices.Count > 0)
            {
                gvcp.CameraIp = devices[0].IP;
            }
            gvsp = new Gvsp(gvcp);
            gvsp.FrameReady += FrameReady;
        }

        private void FrameReady(object sender, byte[] e)
        {
            image = frameProcessor.ByteArrayToBitmapSource(e);
            image.Freeze();
            //Dispatcher.CurrentDispatcher.Invoke(() => RaisePropertyChanged(nameof(Image)), DispatcherPriority.Render);
            RaisePropertyChanged(nameof(Image));
            fpsCount++;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (gvsp.IsStreaming)
            {
                await gvsp.StopStream().ConfigureAwait(false);
            }
            else
            {
                await gvsp.StartStreamAsync().ConfigureAwait(false);
            }
        }
    }
}