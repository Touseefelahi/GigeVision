using Microsoft.VisualStudio.TestTools.UnitTesting;
using GigeVision.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigeVision.Core.Models.Tests
{
    [TestClass()]
    public class GvspTests
    {
        private Gvsp gvsp;
        private Gvcp gvcp;

        public GvspTests()
        {
            Setup();
            while (true)
            {
                if (gvsp != null) break;
                Task.Delay(100);
            }
        }

        [TestMethod()]
        public async Task StartStreamAsyncTest()
        {
            gvsp.FrameReady += FrameReady;
            // await gvsp.StartStreamAsync().ConfigureAwait(false);
            await Task.Delay(5000).ConfigureAwait(false);
            await gvsp.StopStream().ConfigureAwait(false);
        }

        private void FrameReady(object sender, byte[] e)
        {
            throw new NotImplementedException();
        }

        private async void Setup()
        {
            gvcp = new Gvcp() { };
            var devices = await gvcp.GetAllGigeDevicesInNetworkAsnyc();
            gvcp.CameraIp = "192.168.10.196";
            gvsp = new Gvsp(gvcp);
        }
    }
}