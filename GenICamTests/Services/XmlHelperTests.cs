using Microsoft.VisualStudio.TestTools.UnitTesting;
using GenICam;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

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

            Dictionary<string, IGenCategory> catagoryDictionary = new Dictionary<string, IGenCategory>();
            XmlHelper xmlHelper = new XmlHelper("Category", xml);

            var width = xmlHelper.CategoryDictionary["ImageSizeControl"].PFeatures["Width"] as GenInteger;

            var value = width.GetValue();
        }
    }
}