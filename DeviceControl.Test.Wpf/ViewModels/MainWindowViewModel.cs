using DeviceControl.Wpf.ViewModels;
using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using Prism.Mvvm;

namespace DeviceControl.Test.Wpf.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "DeviceControl Test";

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public DeviceControlViewModel DeviceControl { get; set; }

        public MainWindowViewModel()
        {
            IGvcp gvcp = new Gvcp("192.168.10.244");
            DeviceControl = new DeviceControlViewModel(gvcp);
        }
    }
}