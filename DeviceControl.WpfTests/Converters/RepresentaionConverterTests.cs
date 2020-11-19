using Microsoft.VisualStudio.TestTools.UnitTesting;
using DeviceControl.Wpf.Converters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Globalization;

namespace DeviceControl.Wpf.Converters.Tests
{
    [TestClass()]
    public class RepresentaionConverterTests
    {
        [TestMethod()]
        public void ConvertTest()
        {
            RepresentaionConverter representaionConverter = new RepresentaionConverter();

            long data = 485898621;
            byte[] bytes = BitConverter.GetBytes(data);
            var ss = Encoding.ASCII.GetString(bytes);
            // var result = representaionConverter.Convert(ip, null, null, null);
        }
    }
}

//A664A00