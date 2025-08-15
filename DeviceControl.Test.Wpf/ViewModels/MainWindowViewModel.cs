using DeviceControl.Wpf.ViewModels;
using GenICam;
using GigeVision.Core.Interfaces;
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
        public ICamera Camera { get; }

        public MainWindowViewModel()
        {
            string IP1 = "169.254.75.82";
            string IP2 = "192.168.10.244";
            DeviceControl = new DeviceControlViewModel(IP1);
        }
    }
}