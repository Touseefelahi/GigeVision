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
            gvcp.CameraIp = "192.168.10.196";
            gvsp = new Gvsp(gvcp);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await gvsp.StartStreamAsync().ConfigureAwait(false);
        }
    }
}