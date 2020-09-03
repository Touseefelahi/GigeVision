using GigeVision.Core.Enums;
using GigeVision.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace GigeVision.Core.Services
{
    public static class Helper
    {
        private static string GroupComment { get; set; }
        private static string NamespaceName { get; set; } = "ns";
        private static string NamespacePrefix { get; set; } = string.Empty;
        private static XmlNamespaceManager XmlNamespaceManager { get; set; } = null;

        public static void SearchForRegisterbyTagNameInXmlFile(out Dictionary<string, CameraRegister> registerDictionary, string tagName, XmlDocument xmlDocument)
        {
            registerDictionary = new Dictionary<string, CameraRegister>();
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

            var nodeList = xmlDocument.DocumentElement.GetElementsByTagName(tagName);
            if (nodeList != null)
            {
                foreach (XmlNode childNodes in nodeList)
                {
                    GroupComment = childNodes.Attributes["Comment"].Value;

                    foreach (XmlNode childNode in childNodes.ChildNodes)
                    {
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

        private static CameraRegister GetCameraRegisterFromNode(XmlNode xmlNode, string registerName)
        {
            try
            {
                string? description = xmlNode.SelectSingleNode(NamespacePrefix + "Description", XmlNamespaceManager) != null ? xmlNode.SelectSingleNode(NamespacePrefix + "Description", XmlNamespaceManager).InnerText : null;
                CameraRegisterVisibilty? visibilty = xmlNode.SelectSingleNode(NamespacePrefix + "Visibility", XmlNamespaceManager) != null ? ((CameraRegisterVisibilty?)Enum.Parse(typeof(CameraRegisterVisibilty), xmlNode.SelectSingleNode(NamespacePrefix + "Visibility", XmlNamespaceManager).InnerText)) : null;
                if (xmlNode.SelectSingleNode(NamespacePrefix + "Address", XmlNamespaceManager) == null)
                    return null;
                var address = xmlNode.SelectSingleNode(NamespacePrefix + "Address", XmlNamespaceManager).InnerText;
                var length = UInt32.Parse(xmlNode.SelectSingleNode(NamespacePrefix + "Length", XmlNamespaceManager).InnerText);
                var isStreamable = xmlNode.SelectSingleNode(NamespacePrefix + "Streamable", XmlNamespaceManager) != null ? true : false;
                var accessMode = (CameraRegisterAccessMode)Enum.Parse(typeof(CameraRegisterAccessMode), xmlNode.SelectSingleNode(NamespacePrefix + "AccessMode", XmlNamespaceManager).InnerText);
                var type = CameraRegisterType.String;
                return new CameraRegister(registerName, description, visibilty, address, length, accessMode, isStreamable, type, GroupComment);
            }
            catch
            {
                return null;
            }
        }
    }
}