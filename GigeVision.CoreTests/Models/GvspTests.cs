using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GigeVision.Core.Models.Tests
{
    [TestClass()]
    public class GvspTests
    {
        //private Gvcp gvcp;

        //public GvspTests()
        //{
        //    Setup();
        //    while (true)
        //    {
        //        if (gvsp != null)
        //        {
        //            break;
        //        }

        //        Task.Delay(100);
        //    }
        //}

        //[TestMethod()]
        //public async Task StartStreamAsyncTest()
        //{
        //    gvsp.FrameReady += FrameReady;
        //    await gvsp.StartStreamAsync().ConfigureAwait(false);
        //    await Task.Delay(5000).ConfigureAwait(false);
        //    await gvsp.StopStream().ConfigureAwait(false);
        //}

        //[TestMethod()]
        //public async Task SetOffsetAsyncTest()
        //{
        //    await gvsp.Gvcp.ReadAllRegisterAddressFromCameraAsync();
        //    Assert.IsTrue(await gvsp.SetOffsetAsync(0, 0));
        //}

        //private void FrameReady(object sender, byte[] e)
        //{
        //    throw new NotImplementedException();
        //}

        //private async void Setup()
        //{
        //    gvcp = new Gvcp() { };
        //    List<CameraInformation> devices = await gvcp.GetAllGigeDevicesInNetworkAsnyc();
        //    gvcp.CameraIp = "192.168.10.196";
        //    gvsp = new Gvsp(gvcp);
        //}
    }
}