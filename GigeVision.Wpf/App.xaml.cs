using Prism.Ioc;
using GigeVision.Wpf.Views;
using System.Windows;
using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using Prism.DryIoc;
using DryIoc;

namespace GigeVision.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>

    public partial class App
    {
        public static IContainer Container { get; private set; }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            Container = containerRegistry.GetContainer();
            containerRegistry.RegisterSingleton<IGvcp, Gvcp>();

            Container.Resolve<IGvcp>().CameraIp = "192.168.10.244";
        }
    }
}