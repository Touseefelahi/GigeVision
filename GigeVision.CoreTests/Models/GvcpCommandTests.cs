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
    public class GvcpCommandTests
    {
        [TestMethod()]
        public void GenerateCommandTest()
        {
            GvcpCommand gvcpCommand = new GvcpCommand(Converter.RegisterStringToByteArray(GvcpRegister.CCP.ToString("X")), GvcpCommandType.Read);
        }

        [TestMethod()]
        public void GenerateCommandTest1()
        {
        }
    }
}