using Microsoft.VisualStudio.TestTools.UnitTesting;
using GigeVision.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GigeVision.Core.Enums;

namespace GigeVision.Core.Models.Tests
{
    [TestClass()]
    public class ConverterTests
    {
        [TestMethod()]
        public void RegisterStringToByteArrayTest()
        {
            var bytes0 = Converter.RegisterStringToByteArray("1245");
            CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x12, 0x45 }, bytes0);
            var bytes1 = Converter.RegisterStringToByteArray("0x1245");
            CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x12, 0x45 }, bytes1);
            var bytes2 = Converter.RegisterStringToByteArray("A045456");
            CollectionAssert.AreEqual(new byte[] { 0x0A, 0x04, 0x54, 0x56 }, bytes2);
            //var bytes3 = Converter.RegisterStringToByteArray(GigeVision.Core.GvcpRegister.CCP.ToString("X"));
            //CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x0A, 0x00 }, bytes3);
            var bytes4 = Converter.RegisterStringToByteArray("9978");
            CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x99, 0x78 }, bytes4);
        }
    }
}