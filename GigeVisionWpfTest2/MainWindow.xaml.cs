using GigeVision.Core.Models;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System;
using System.Collections.Generic;

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
            camera.IP = "192.168.10.77";
            camera.FrameReady += FrameReady;
            camera.Updates += Updates;
            image.WidthImage = 640;
            image.HeightImage = 512;
            camera.Payload = 640 * 2 * 7;
            timer = new();
            timer.Elapsed += Timer_Elapsed;
            timer.Interval = 1000;
            PixelFormat format = PixelFormats.Gray16;
            const double dpi = 96;
            List<Color> colors = new List<Color> { Colors.Gray };
            BitmapPalette myPalette = new BitmapPalette(colors);
            SourceImage = new WriteableBitmap(image.WidthImage, image.HeightImage, dpi, dpi, format, myPalette);
            RectBitmap = new Int32Rect(0, 0, image.WidthImage, image.HeightImage);
        }

        private void Updates(object sender, string e)
        {

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
        int count = 0;
        WriteableBitmap SourceImage;

        public Int32Rect RectBitmap { get; }
        List<ulong> list = new();
        private void FrameReady(object sender, byte[] e)
        {
            list.Add((ulong)sender );
            //byte[] clonee = (byte[])e.Clone();
            image.Dispatcher.Invoke(() => image.RawBytes = e);
            //SourceImage.WritePixels(RectBitmap, RawBytes, image.WidthImage * 2, 0);          
            FrameCounter++;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();
            await camera.StartStreamAsync("192.168.10.227").ConfigureAwait(false);
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