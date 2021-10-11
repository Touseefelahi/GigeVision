using Prism.Ioc;
using DeviceControl.Test.Wpf.Views;
using System.Windows;
using GenICam;
using GigeVision.Core;
using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;

namespace DeviceControl.Test.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            //containerRegistry.RegisterInstance<ICamera>(new Camera(new GenPort(new )));
        }
    }
}