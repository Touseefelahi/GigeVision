using DeviceControl.Wpf.ViewModels;
using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace GigeVision.Wpf.ViewModels.Tests
{
    [TestClass()]
    public class DeviceControlViewModelTests
    {
        public DeviceControlViewModelTests()
        {
            Gvcp = new Gvcp("192.168.10.244");
            DeviceControl = new DeviceControlViewModel(Gvcp);
        }

        private IGvcp Gvcp { get; set; }
        private DeviceControlViewModel DeviceControl { get; set; }

        [TestMethod()]
        public async Task DeviceControlViewModelTest()
        {
            await DeviceControl.Gvcp.ReadAllRegisterAddressFromCameraAsync().ConfigureAwait(false);
            DeviceControl.CreateCameraRegistersGroupCollection();
            var deviceControlList = DeviceControl.CameraRegisterGroupDTOList;

            CollectionAssert.AllItemsAreNotNull(deviceControlList);
        }
    }
}