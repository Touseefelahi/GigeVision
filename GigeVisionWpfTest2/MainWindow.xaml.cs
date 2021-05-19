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
            gvcp = new();
            Button_Click(null, null);
        }

        public int MyProperty { get; set; }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var devices = await gvcp.GetAllGigeDevicesInNetworkAsnyc().ConfigureAwait(false);
            await (_ = gvcp.ReadRegisterAsync("192.168.10.77", GigeVision.Core.Enums.GvcpRegister.CCP)).ConfigureAwait(false);
            Dispatcher.Invoke(() =>
            {
                IP.Text = camera.GetMyIp();
                Count.Text = devices.Count.ToString();
            });
        }
    }
}