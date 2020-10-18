using Microsoft.VisualStudio.TestTools.UnitTesting;
using GigeVision.Wpf.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using System.Threading.Tasks;

namespace GigeVision.Wpf.ViewModels.Tests
{
    [TestClass()]
    public class DeviceControlViewModelTests
    {
        private IGvcp Gvcp { get; set; }
        private DeviceControlViewModel DeviceControl { get; set; }

        public DeviceControlViewModelTests()
        {
            Gvcp = new Gvcp("192.168.10.244");
            DeviceControl = new DeviceControlViewModel(Gvcp);
        }

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