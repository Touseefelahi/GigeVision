using System.Xml;
using Xunit;
using GenICam;
using GigeVision.Core;
using GigeVision.Core.Models;

namespace GenICam.Tests
{
    public class Helper
    {
        [Fact]
        public void XMLParser()
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load("XMLSample_Breakfast.xml");
            //xmlDocument.Load(filename: "Imperx.xml");


            var xnsm = new XmlNamespaceManager(xmlDocument.NameTable);
            var rootNode = xmlDocument["breakfast_menu"];
            var uri = rootNode.Attributes["xmlns"].Value;
            var ns = rootNode.Attributes["StandardNameSpace"].Value;
            xnsm.AddNamespace(ns, uri);
            var node = xmlDocument.SelectSingleNode($"//*[@Name='Sa']", xnsm);
        }


        [Fact]
        public async void XMLHelper()
        {

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load("Imperx.xml");
            var genPort = new GenPort(new Gvcp());
            var xmlHelper = new XmlHelper(xmlDocument, genPort);
             await xmlHelper.LoadUp();
        }

    }
}