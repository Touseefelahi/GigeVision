using GigeVision.Core.Enums;
using GigeVision.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace GigeVision.Core.Services
{
    public class XmlHelper
    {
        private string NamespaceName { get; set; } = "ns";
        private string NamespacePrefix { get; set; } = string.Empty;
        private XmlNamespaceManager XmlNamespaceManager { get; set; } = null;
        private XmlDocument XmlDocument { get; set; } = null;

        public void SearchForRegisterbyTagNameInXmlFile(out Dictionary<string, CameraRegister> registerDictionary, out Dictionary<string, CameraRegisterGroup> regisetrGroupDictionary, string tagName, XmlDocument xmlDocument)
        {
            registerDictionary = new Dictionary<string, CameraRegister>();
            regisetrGroupDictionary = new Dictionary<string, CameraRegisterGroup>();
            var root = xmlDocument.FirstChild.NextSibling;
            if (root.Attributes != null)
            {
                var xmlns = root.Attributes["xmlns"];
                if (xmlns != null)
                {
                    XmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
                    XmlNamespaceManager.AddNamespace(NamespaceName, xmlns.Value);
                    NamespacePrefix = $"{NamespaceName}:";
                }
            }

            XmlDocument = xmlDocument;
            var nodeList = xmlDocument.DocumentElement.GetElementsByTagName(tagName);
            if (nodeList != null)
            {
                foreach (XmlNode childNodes in nodeList)
                {
                    //Poor Logic, repeating these following values in every CameraRegister Object
                    //ToDo: Improve Camera Register Group Logic;
                    string groupComment = childNodes.Attributes["Comment"].Value;
                    List<string> categoryFeatures = new List<string>();

                    foreach (XmlNode childNode in childNodes.ChildNodes)
                    {
                        if (childNode.Name == "Category")
                        {
                            foreach (XmlNode feature in childNode.ChildNodes)
                            {
                                categoryFeatures.Add(feature.InnerText);
                            }

                            regisetrGroupDictionary.Add(groupComment, new CameraRegisterGroup(groupComment, categoryFeatures));
                        }

                        if (childNode.Name == "StringReg")
                        {
                            string registerName = childNode.Attributes["Name"].Value;
                            var cameraRegister = GetCameraRegisterFromNode(childNode, registerName);
                            if (cameraRegister != null)
                                registerDictionary.Add(registerName, cameraRegister);
                        }
                        else if (childNode.Name == "IntReg")
                        {
                            string registerName = childNode.Attributes["Name"].Value;
                            var cameraRegister = GetCameraRegisterFromNode(childNode, registerName);
                            if (cameraRegister != null)
                                registerDictionary.Add(registerName, cameraRegister);
                        }
                        else if (childNode.Name == "MaskedIntReg")
                        {
                            string registerName = childNode.Attributes["Name"].Value;
                            var cameraRegister = GetCameraRegisterFromNode(childNode, registerName);

                            if (cameraRegister != null)
                                registerDictionary.Add(registerName, cameraRegister);
                        }
                    }
                }
            }
        }

        private CameraRegister GetCameraRegisterFromNode(XmlNode xmlNode, string registerName)
        {
            try
            {
                if (xmlNode.SelectSingleNode(NamespacePrefix + "Address", XmlNamespaceManager) == null)
                    return null;
                if (!xmlNode.SelectSingleNode(NamespacePrefix + "Address", XmlNamespaceManager).InnerText.StartsWith("0x"))
                    return null;
                var address = xmlNode.SelectSingleNode(NamespacePrefix + "Address", XmlNamespaceManager).InnerText;
                var length = UInt32.Parse(xmlNode.SelectSingleNode(NamespacePrefix + "Length", XmlNamespaceManager).InnerText);
                var accessMode = (CameraRegisterAccessMode)Enum.Parse(typeof(CameraRegisterAccessMode), xmlNode.SelectSingleNode(NamespacePrefix + "AccessMode", XmlNamespaceManager).InnerText);

                CameraRegisterType cameraRegisterType = CameraRegisterType.Integer;
                switch (xmlNode.Name)
                {
                    case "StringReg":
                        cameraRegisterType = CameraRegisterType.String;
                        break;

                    case "IntReg":
                        cameraRegisterType = CameraRegisterType.Integer;
                        break;

                    case "MaskedIntReg":
                        cameraRegisterType = CameraRegisterType.Integer;
                        break;

                    default:
                        break;
                }

                if (xmlNode.Attributes["Name"].Value.EndsWith("Reg"))
                    xmlNode = xmlNode.PreviousSibling;

                string? description = xmlNode.SelectSingleNode(NamespacePrefix + "Description", XmlNamespaceManager) != null ? xmlNode.SelectSingleNode(NamespacePrefix + "Description", XmlNamespaceManager).InnerText : null;
                CameraRegisterVisibilty? visibilty = xmlNode.SelectSingleNode(NamespacePrefix + "Visibility", XmlNamespaceManager) != null ? ((CameraRegisterVisibilty?)Enum.Parse(typeof(CameraRegisterVisibilty), xmlNode.SelectSingleNode(NamespacePrefix + "Visibility", XmlNamespaceManager).InnerText)) : null;
                var isStreamable = xmlNode.SelectSingleNode(NamespacePrefix + "Streamable", XmlNamespaceManager) != null ? true : false;

                Dictionary<string, int> enumeration = new Dictionary<string, int>();

                if (xmlNode.Name == "Enumeration")
                {
                    var enumList = xmlNode.SelectNodes(NamespacePrefix + "EnumEntry", XmlNamespaceManager);
                    foreach (XmlNode enumEntry in enumList)
                    {
                        enumeration.Add(enumEntry.Attributes["Name"].Value, Int32.Parse(enumEntry.SelectSingleNode(NamespacePrefix + "Value", XmlNamespaceManager).InnerText));
                    }
                }

                return new CameraRegister(registerName, description, visibilty, address, length, accessMode, isStreamable, cameraRegisterType, enumeration);
            }
            catch
            {
                return null;
            }
        }
    }
}