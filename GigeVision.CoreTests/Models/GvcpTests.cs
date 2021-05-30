using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GigeVision.Core.Models.Tests
{
    [TestClass()]
    public class GvcpTests
    {
        private readonly Gvcp gvcp;
        private readonly string ipCamera = "192.168.10.244";

        public GvcpTests()
        {
            gvcp = new Gvcp() { CameraIp = "192.168.10.244" };
        }

        [TestMethod()]
        public async Task ReadAllRegisterAddressFromCameraAsyncTestAsync()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Dictionary<string, string> registers = await gvcp.ReadAllRegisterAddressFromCameraAsync().ConfigureAwait(false);
            stopwatch.Stop();
            long timeTook = stopwatch.ElapsedMilliseconds;
            if (registers.Count > 10)
            {
                bool isTrue = registers.ContainsKey("AcquisitionStart");
                string startRegister = registers["AcquisitionStart"];
                Assert.IsTrue(isTrue);
            }
        }

        [TestMethod()]
        public async Task ReadRegisterAsyncTest()
        {
            GvcpReply value = await gvcp.ReadRegisterAsync(ipCamera, Enums.GvcpRegister.CCP).ConfigureAwait(false);
            if (value.IsSentAndReplyReceived)
            {
                Assert.IsTrue(value.IsValid);
            }

            GvcpReply value2 = await gvcp.ReadRegisterAsync(Enums.GvcpRegister.CCP).ConfigureAwait(false);
            if (value2.IsSentAndReplyReceived)
            {
                Assert.IsTrue(value2.IsValid);
            }

            GvcpReply value3 = await gvcp.ReadRegisterAsync("0xA00").ConfigureAwait(false);
            if (value3.IsSentAndReplyReceived)
            {
                Assert.IsTrue(value3.IsValid);
            }
        }

        [TestMethod()]
        public async Task GetAllGigeDevicesInNetworkAsnycTest()
        {
            List<CameraInformation> devices = await gvcp.GetAllGigeDevicesInNetworkAsnyc().ConfigureAwait(false);
            if (devices.Count > 0)
            {
                Assert.IsTrue(true);
            }
        }

        [TestMethod()]
        public async Task WriteRegisterAsyncTest()
        {
            // var registers = await gvcp.ReadAllRegisterAddressFromCameraAsync().ConfigureAwait(false);
            GvcpReply reply = await gvcp.WriteRegisterAsync(Enums.GvcpRegister.CCP, 2).ConfigureAwait(false);
            // var repply = await gvcp.WriteRegisterAsync(ipCamera, Enums.GvcpRegister.SCDA, 1);
            await Task.Delay(1000).ConfigureAwait(false);
            //var reply = await gvcp.WriteRegisterAsync(registers["AcquisitionStartReg"], 1);
        }

        [TestMethod()]
        public async Task TakeControlTest()
        {
            bool controlLeft1 = await gvcp.LeaveControl().ConfigureAwait(false);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            bool isInControl = await gvcp.TakeControl(true).ConfigureAwait(false);
            TimeSpan msToTakeControl = stopwatch.Elapsed;
            stopwatch.Reset();
            if (isInControl)
            {
                await Task.Delay(15000).ConfigureAwait(false);
                stopwatch.Start();
                bool controlLeft = await gvcp.LeaveControl().ConfigureAwait(false);
                TimeSpan msToLeaveControl = stopwatch.Elapsed;
            }
        }
    }
}