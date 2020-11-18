using DeviceControl.Wpf.ViewModels;
using GenICam;
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
        public IGenPort GenPort { get; }

        public MainWindowViewModel(IGenPort genPort)
        {
            GenPort = genPort;
            DeviceControl = new DeviceControlViewModel(genPort);
        }
    }
}