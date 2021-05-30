using GigeVision.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace GigeVisionWpfTest2
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