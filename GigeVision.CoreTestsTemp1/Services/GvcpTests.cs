using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GigeVision.Core.Models.Tests
{
    [TestClass()]
    public class GvcpTests
    {
        [TestMethod()]
        public async Task GetAllGigeDevicesInNetworkAsnycTest()
        {
            Gvcp gvcp = new();
            List<CameraInformation> devices = await gvcp.GetAllGigeDevicesInNetworkAsnyc();
            Assert.Fail();
        }
    }
}