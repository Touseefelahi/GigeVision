using Microsoft.VisualStudio.TestTools.UnitTesting;
using GenICam;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using GigeVision.Core.Models;
using GigeVision.Core;

namespace GenICam.Tests
{
    [TestClass()]
    public class XmlHelperTests
    {
        [TestMethod()]
        public void GetStringCategoryTest()
        {
        }

        [TestMethod()]
        public void XmlHelperTest()
        {
            XmlDocument xml = new XmlDocument();
            xml.Load("GEV_B1020C_v209.xml");

            Gvcp gvcp = new Gvcp(new GenPort(3956));
            XmlHelper xmlHelper = new XmlHelper("Category", xml, gvcp.GenPort);
            //var width = xmlHelper.CategoryDictionary["ImageSizeControl"].PFeatures["Width"] as GenInteger;

            //var value = width.GetValue();
        }
    }
}