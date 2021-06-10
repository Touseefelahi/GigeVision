using GigeVision.Core.Enums;
using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace GigeVision.Core.Services
{
    /// <summary>
    /// this class helps Gvcp to read all the registers from xml file
    /// </summary>
    public class XmlHelper
    {
        /// <summary>
        /// the main method to read xml file
        /// </summary>
        /// <param name="registerDictionary">Register Dictionary</param>
        /// <param name="regisetrGroupDictionary">Register Group Dictionary</param>
        /// <param name="tagName">First Parent Tag Name</param>
        /// <param name="xmlDocument">Xml File</param>
        public XmlHelper(out Dictionary<string, CameraRegisterContainer> registerDictionary, out Dictionary<string, CameraRegisterGroup> regisetrGroupDictionary, string tagName, XmlDocument xmlDocument)
        {
            registerDictionary = new Dictionary<string, CameraRegisterContainer>();
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

            var nodeList = XmlDocument.DocumentElement.GetElementsByTagName(tagName);
            if (nodeList != null)
            {
                foreach (XmlNode childNodes in nodeList)
                {
                    string groupComment = childNodes.Attributes["Comment"].Value;
                    List<string> categoryFeatures = new();

                    foreach (XmlNode childNode in childNodes.ChildNodes)
                    {
                        try
                        {
                            if (childNode.Name == "Category")
                            {
                                foreach (XmlNode feature in childNode.ChildNodes)
                                {
                                    categoryFeatures.Add(feature.InnerText);
                                }
                                regisetrGroupDictionary.Add(groupComment, new CameraRegisterGroup(groupComment, categoryFeatures));
                            }
                            if (!childNode.Name.Equals("Category"))
                            {
                                string registerName = childNode.Attributes["Name"].Value;
                                var cameraRegisterContainer = GetCameraRegisterContainerFromNode(childNode);
                                if (cameraRegisterContainer != null)
                                    registerDictionary.Add(registerName, cameraRegisterContainer);
                            }
                        }
                        catch (Exception ex)
                        {
                            var error = ex;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// the main method to read xml file
        /// </summary>
        /// <param name="gvcp">Gvcp</param>
        /// <param name="registerDictionary">Regiser Dictionary</param>
        /// <param name="regisetrGroupDictionary">Register Group Dictionary</param>
        /// <param name="tagName">First Parent Tag Name</param>
        /// <param name="xmlDocument">Xml File</param>
        /// &gt;
        public XmlHelper(IGvcp gvcp, out Dictionary<string, CameraRegisterContainer> registerDictionary, out Dictionary<string, CameraRegisterGroup> regisetrGroupDictionary, string tagName, XmlDocument xmlDocument)
        {
            Gvcp = gvcp;
            registerDictionary = new Dictionary<string, CameraRegisterContainer>();
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

            //Types of Nodes
            //1- Group
            //2- Category
            //3- StringReg
            //4- Enumeration
            //5- Integer
            //6- IntReg
            //7- Command
            //8- IntSwissKnife
            //9- Boolean

            var nodeList = XmlDocument.DocumentElement.GetElementsByTagName(tagName);
            if (nodeList != null)
            {
                foreach (XmlNode childNodes in nodeList)
                {
                    string groupComment = childNodes.Attributes["Comment"].Value;
                    List<string> categoryFeatures = new List<string>();

                    foreach (XmlNode childNode in childNodes.ChildNodes)
                    {
                        try
                        {
                            if (childNode.Name == "Category")
                            {
                                foreach (XmlNode feature in childNode.ChildNodes)
                                {
                                    categoryFeatures.Add(feature.InnerText);
                                }
                                regisetrGroupDictionary.Add(groupComment, new CameraRegisterGroup(groupComment, categoryFeatures));
                            }
                            if (!childNode.Name.Equals("Category"))
                            {
                                string registerName = childNode.Attributes["Name"].Value;
                                var cameraRegisterContainer = GetCameraRegisterContainerFromNode(childNode);
                                if (cameraRegisterContainer != null)
                                    registerDictionary.Add(registerName, cameraRegisterContainer);
                            }
                        }
                        catch (Exception ex)
                        {
                            var error = ex;
                        }
                    }
                }
            }
        }

        private string NamespaceName { get; } = "ns";
        private string NamespacePrefix { get; } = string.Empty;
        private XmlNamespaceManager XmlNamespaceManager { get; set; } = null;
        private XmlDocument XmlDocument { get; set; } = null;

        private IGvcp Gvcp { get; set; }

        #region Helpers

        private CameraRegisterContainer GetCameraRegisterContainerFromNode(XmlNode xmlNode)
        {
            CameraRegisterContainer cameraRegisterContainer = null;

            switch (xmlNode.Name)
            {
                case nameof(CameraRegisterType.StringReg):
                    //StringReg Has no pValue node
                    cameraRegisterContainer = GetCameraRegisterContainer(xmlNode, CameraRegisterType.StringReg);
                    break;

                case nameof(CameraRegisterType.Integer):
                    cameraRegisterContainer = GetCameraRegisterContainer(xmlNode, CameraRegisterType.Integer);
                    break;

                case nameof(CameraRegisterType.Enumeration):
                    cameraRegisterContainer = GetCameraRegisterContainer(xmlNode, CameraRegisterType.Enumeration);
                    break;

                case nameof(CameraRegisterType.Command):
                    cameraRegisterContainer = GetCameraRegisterContainer(xmlNode, CameraRegisterType.Command);

                    break;

                case nameof(CameraRegisterType.Float):
                    cameraRegisterContainer = GetCameraRegisterContainer(xmlNode, CameraRegisterType.Float);

                    break;

                case nameof(CameraRegisterType.Boolean):
                    cameraRegisterContainer = GetCameraRegisterContainer(xmlNode, CameraRegisterType.Boolean);
                    break;

                default:
                    break;
            }

            return cameraRegisterContainer;
        }

        private XmlNode GetNodeByAttirbuteValue(XmlNode parentNode, string tagName, string value)
        {
            return parentNode.SelectSingleNode($"{NamespacePrefix}{tagName}[@Name='{value}']", XmlNamespaceManager);
        }

        private object ReadPNode(XmlNode parentNode, string pNode)
        {
            //var test = parentNode.SelectSingleNode(NamespacePrefix + "IntReg[@Name='DeviceScanTypeReg']", XmlNamespaceManager);

            //pNode Value is an IntReg
            if (GetNodeByAttirbuteValue(parentNode, "IntReg", pNode) is XmlNode intRegNode)
            {
                var address = intRegNode.SelectSingleNode(NamespacePrefix + "Address", XmlNamespaceManager) != null ? intRegNode.SelectSingleNode(NamespacePrefix + "Address", XmlNamespaceManager).InnerText : null;
                object pAddress = null;
                if (intRegNode.SelectSingleNode(NamespacePrefix + "pAddress", XmlNamespaceManager) is XmlNode xmlNode)
                {
                    pAddress = ReadPNode(parentNode, xmlNode.InnerText);
                }

                var length = uint.Parse(intRegNode.SelectSingleNode(NamespacePrefix + "Length", XmlNamespaceManager).InnerText);
                var accessMode = (CameraRegisterAccessMode)Enum.Parse(typeof(CameraRegisterAccessMode), intRegNode.SelectSingleNode(NamespacePrefix + "AccessMode", XmlNamespaceManager).InnerText);
                return new CameraRegister(address, length, accessMode, pAddress);
            }

            //pNode Value is a SwissKinfe or IntSwissKnife
            else if (GetNodeByAttirbuteValue(parentNode, "SwissKnife", pNode) is XmlNode swissKnifNode)
            {
                Dictionary<string, IntSwissKnife> pVariableSwissKnife = new Dictionary<string, IntSwissKnife>();
                Dictionary<string, string> pVariableRegister = new Dictionary<string, string>();
                Dictionary<string, CameraRegisterContainer> pVariableRegisterContainer = new Dictionary<string, CameraRegisterContainer>();

                string formula = string.Empty;

                foreach (XmlNode child in swissKnifNode.ChildNodes)
                {
                    //child could be either pVariable or Formula
                    var nodeName = child.Attributes["Name"] != null ? child.Attributes["Name"].Value : string.Empty;
                    if (child.Name.Equals("pVariable"))
                    {
                        var pVariable = swissKnifNode.SelectSingleNode(NamespacePrefix + "pVariable", XmlNamespaceManager).InnerText;
                        var pVariableValue = ReadPNode(swissKnifNode.ParentNode, pVariable);

                        if (pVariableValue is null)
                            pVariableValue = ReadPNode(LookForChildInsideAllParents(XmlDocument.DocumentElement, pVariable).ParentNode, pVariable);

                        //pVariable could be IntSwissKnife, SwissKnife, Integer, IntReg, Float, FloatReg,
                        if (pVariableValue is IntSwissKnife intSwissKnife)
                            pVariableSwissKnife.Add(nodeName, intSwissKnife);
                        else if (pVariableValue is CameraRegister cameraRegister)
                            pVariableRegister.Add(nodeName, cameraRegister.Address);
                        else if (pVariableValue is CameraRegisterContainer cameraRegisterContainer)
                            pVariableRegisterContainer.Add(nodeName, cameraRegisterContainer);
                    }
                    else if (child.Name.Equals("Formula"))
                        formula = swissKnifNode.SelectSingleNode(NamespacePrefix + "Formula", XmlNamespaceManager).InnerText;
                }

                if (pVariableSwissKnife.Count > 0)
                    return new IntSwissKnife(Gvcp, formula, pVariableSwissKnife);
                else if (pVariableRegister.Count > 0)
                    return new IntSwissKnife(Gvcp, formula, pVariableRegister);
                else if (pVariableRegisterContainer.Count > 0)
                    return new IntSwissKnife(Gvcp, formula, pVariableRegisterContainer);
            }
            else if (GetNodeByAttirbuteValue(parentNode, "IntSwissKnife", pNode) is XmlNode intSwissKnifNode)
            {
                Dictionary<string, IntSwissKnife> pVariableIntSwissKnife = new Dictionary<string, IntSwissKnife>();
                Dictionary<string, string> pVariableRegister = new Dictionary<string, string>();
                Dictionary<string, CameraRegisterContainer> pVariableRegisterContainer = new Dictionary<string, CameraRegisterContainer>();

                string formula = string.Empty;

                foreach (XmlNode child in intSwissKnifNode.ChildNodes)
                {
                    //child could be either pVariable or Formula
                    var nodeName = child.Attributes["Name"] != null ? child.Attributes["Name"].Value : string.Empty;
                    if (child.Name.Equals("pVariable"))
                    {
                        var pVariable = child.InnerText;
                        var pVariableValue = ReadPNode(intSwissKnifNode.ParentNode, pVariable);

                        if (pVariableValue is null)
                            pVariableValue = ReadPNode(LookForChildInsideAllParents(XmlDocument.DocumentElement, pVariable).ParentNode, pVariable);

                        //pVariable could be IntSwissKnife, SwissKnife, Integer, IntReg, Float, FloatReg,
                        if (pVariableValue is IntSwissKnife intSwissKnife)
                            pVariableIntSwissKnife.Add(nodeName, intSwissKnife);
                        else if (pVariableValue is CameraRegister cameraRegister)
                            pVariableRegister.Add(nodeName, cameraRegister.Address);
                        else if (pVariableValue is CameraRegisterContainer cameraRegisterContainer)
                            pVariableRegisterContainer.Add(nodeName, cameraRegisterContainer);
                    }
                    else if (child.Name.Equals("Formula"))
                        formula = intSwissKnifNode.SelectSingleNode(NamespacePrefix + "Formula", XmlNamespaceManager).InnerText;
                }

                if (pVariableIntSwissKnife.Count > 0)
                {
                    return new IntSwissKnife(Gvcp, formula, pVariableIntSwissKnife);
                }
                else if (pVariableRegister.Count > 0)
                {
                    return new IntSwissKnife(Gvcp, formula, pVariableRegister);
                }
                else if (pVariableRegisterContainer.Count > 0)
                {
                    return new IntSwissKnife(Gvcp, formula, pVariableRegisterContainer);
                }
                else if (formula != string.Empty)
                {
                    return new IntSwissKnife(Gvcp, formula, null);
                }
            }
            else if (GetNodeByAttirbuteValue(parentNode, "Integer", pNode) is XmlNode integerNode)
                return GetCameraRegisterContainerFromNode(integerNode);
            else if (GetNodeByAttirbuteValue(parentNode, "MaskedIntReg", pNode) is XmlNode maskedIntRegNode)
            {
                var pAddress = maskedIntRegNode.SelectSingleNode(NamespacePrefix + "pAddress", XmlNamespaceManager) != null ?
                    maskedIntRegNode.SelectSingleNode(NamespacePrefix + "pAddress", XmlNamespaceManager).InnerText : null;
                var address = maskedIntRegNode.SelectSingleNode(NamespacePrefix + "Address", XmlNamespaceManager) != null ?
                    maskedIntRegNode.SelectSingleNode(NamespacePrefix + "Address", XmlNamespaceManager).InnerText : null;
                var pAddressValue = ReadPNode(maskedIntRegNode.ParentNode, pAddress) as IntSwissKnife;
                var length = uint.Parse(maskedIntRegNode.SelectSingleNode(NamespacePrefix + "Length", XmlNamespaceManager).InnerText);
                var accessMode = (CameraRegisterAccessMode)Enum.Parse(typeof(CameraRegisterAccessMode), maskedIntRegNode.SelectSingleNode(NamespacePrefix + "AccessMode", XmlNamespaceManager).InnerText);
                return new MaskedIntReg(pAddressValue, address, length, accessMode);
            }

            return null;
        }

        private XmlNode LookForChildInsideAllParents(XmlNode firstParent, string childName)
        {
            foreach (XmlNode parent in firstParent.ChildNodes)
            {
                foreach (XmlNode child in parent.ChildNodes)
                {
                    if (child.Attributes != null)
                        if (child.Attributes["Name"] != null)
                            if (child.Attributes["Name"].Value == childName)
                                return child;
                }
            }

            return null;
        }

        private CameraRegisterContainer GetCameraRegisterContainer(XmlNode xmlNode, CameraRegisterType cameraRegisterType)
        {
            object cameraRegisterTypeValue = null;

            switch (cameraRegisterType)
            {
                case CameraRegisterType.StringReg:
                    string address = xmlNode.SelectSingleNode(NamespacePrefix + "Address", XmlNamespaceManager).InnerText;
                    uint length = uint.Parse(xmlNode.SelectSingleNode(NamespacePrefix + "Length", XmlNamespaceManager).InnerText);
                    CameraRegisterAccessMode accessMode = (CameraRegisterAccessMode)Enum.Parse(typeof(CameraRegisterAccessMode), xmlNode.SelectSingleNode(NamespacePrefix + "AccessMode", XmlNamespaceManager).InnerText);

                    cameraRegisterTypeValue = new CameraRegister(address, length, accessMode);
                    break;

                case CameraRegisterType.Integer:
                    uint? value = null;
                    if (xmlNode.Attributes["Name"].Value.EndsWith("Expr") || xmlNode.Attributes["Name"].Value.EndsWith("Val"))
                    {
                        value = uint.Parse(xmlNode.SelectSingleNode(NamespacePrefix + "Value", XmlNamespaceManager).InnerText);

                        cameraRegisterTypeValue = new IntegerRegister(null, null, null, value);
                    }

                    uint? min = null, max = null, inc = null;
                    IntSwissKnife pMin = null, pMax = null;
                    string pNode;
                    object pNodeValue = null;

                    foreach (XmlNode node in xmlNode.ChildNodes)
                    {
                        switch (node.Name)
                        {
                            case "Value":
                                value = uint.Parse(node.InnerText);

                                break;

                            case "Min":
                                min = uint.Parse(node.InnerText);
                                break;

                            case "Max":
                                max = uint.Parse(node.InnerText);
                                break;

                            case "pMin":
                                pNode = node.InnerText;
                                pMin = ReadPNode(xmlNode.ParentNode, pNode) as IntSwissKnife;
                                break;

                            case "pMax":
                                pNode = node.InnerText;
                                pMax = ReadPNode(xmlNode.ParentNode, pNode) as IntSwissKnife;

                                break;

                            case "Inc":
                                inc = uint.Parse(node.InnerText);
                                break;

                            case "pValue":
                                pNode = node.InnerText;
                                pNodeValue = ReadPNode(xmlNode.ParentNode, pNode);
                                break;

                            default:
                                break;
                        }
                    }

                    //Find pValue
                    if (pNodeValue is CameraRegister integerCameraRegister)
                        cameraRegisterTypeValue = new IntegerRegister(min, max, inc, value, integerCameraRegister, integerCameraRegister, pMin, pMax);
                    else if (pNodeValue is IntSwissKnife intSwissKnifeRegister)
                        cameraRegisterTypeValue = new IntegerRegister(min, max, inc, value, null, intSwissKnifeRegister, pMin, pMax);
                    //else if (pNodeValue is MaskedIntReg maskedIntReg)
                    //    cameraRegisterTypeValue = new IntegerRegister(min, max, inc, value, null, maskedIntReg, pMin, pMax);
                    else if (pNodeValue is MaskedIntReg maskedIntReg)
                    {
                        CameraRegister cameraRegister = new CameraRegister(maskedIntReg.Address, maskedIntReg.Length, maskedIntReg.AccessMode, maskedIntReg.Value, maskedIntReg.AddressParameter);
                        cameraRegisterTypeValue = new IntegerRegister(min, max, inc, value, cameraRegister, maskedIntReg, pMin, pMax);
                    }
                    break;

                case CameraRegisterType.Float:
                    IntSwissKnife floatMin = null, floatMax = null, floatValue = null;
                    PhysicalUnit? physicalUnit = null;

                    Dictionary<string, CameraRegisterContainer> cameraRegistersContainerDictionary = new Dictionary<string, CameraRegisterContainer>();

                    foreach (XmlNode node in xmlNode.ChildNodes)
                    {
                        switch (node.Name)
                        {
                            case "pValue":
                                //Find pValue
                                pNode = node.InnerText;
                                pNodeValue = ReadPNode(xmlNode.ParentNode, pNode);
                                if (pNodeValue is IntSwissKnife pValueIntSwissKnife)
                                    floatValue = pValueIntSwissKnife;
                                break;

                            case "pMin":
                                //Find pMin
                                pNode = node.InnerText;
                                pNodeValue = ReadPNode(xmlNode.ParentNode, pNode);
                                if (pNodeValue is IntSwissKnife pMinIntSwissKnife)
                                    floatMin = pMinIntSwissKnife;
                                break;

                            case "pMax":
                                //Find pMax
                                pNode = node.InnerText;
                                pNodeValue = ReadPNode(xmlNode.ParentNode, pNode);
                                if (pNodeValue is IntSwissKnife pMaxIntSwissKnife)
                                    floatMax = pMaxIntSwissKnife;
                                break;

                            case "Unit":
                                //Find pMax
                                physicalUnit = (PhysicalUnit?)Enum.Parse(typeof(PhysicalUnit), node.InnerText);
                                break;

                            default:
                                break;
                        }
                    }

                    cameraRegisterTypeValue = new FloatRegister(floatValue, floatMin, floatMax, physicalUnit);
                    break;

                case CameraRegisterType.Enumeration:
                    Dictionary<string, uint> entry = new Dictionary<string, uint>();
                    var enumList = xmlNode.SelectNodes(NamespacePrefix + "EnumEntry", XmlNamespaceManager);

                    foreach (XmlNode enumEntry in enumList)
                        entry.Add(enumEntry.Attributes["Name"].Value, UInt32.Parse(enumEntry.SelectSingleNode(NamespacePrefix + "Value", XmlNamespaceManager).InnerText));

                    //Find pValue
                    pNode = xmlNode.SelectSingleNode(NamespacePrefix + "pValue", XmlNamespaceManager).InnerText;
                    pNodeValue = ReadPNode(xmlNode.ParentNode, pNode);
                    if (pNodeValue is CameraRegister enumCameraRegister)
                        cameraRegisterTypeValue = new Enumeration(entry, enumCameraRegister);

                    break;

                case CameraRegisterType.Command:
                    uint commandValue = uint.Parse(xmlNode.SelectSingleNode(NamespacePrefix + "CommandValue", XmlNamespaceManager).InnerText);

                    //Find pValue
                    pNode = xmlNode.SelectSingleNode(NamespacePrefix + "pValue", XmlNamespaceManager).InnerText;
                    pNodeValue = ReadPNode(xmlNode.ParentNode, pNode);
                    if (pNodeValue is CameraRegister commandCameraRegister)
                        cameraRegisterTypeValue = new CommandRegister(commandValue, commandCameraRegister);

                    break;

                case CameraRegisterType.Boolean:

                    //Find pValue
                    pNode = xmlNode.SelectSingleNode(NamespacePrefix + "pValue", XmlNamespaceManager).InnerText;
                    pNodeValue = ReadPNode(xmlNode.ParentNode, pNode);
                    if (pNodeValue is CameraRegister booleanCameraRegister)
                        cameraRegisterTypeValue = new BooleanRegister(booleanCameraRegister);
                    break;

                default:
                    break;
            }

            //Register Container nodes
            string registerName = xmlNode.Attributes["Name"].Value;
            string description = xmlNode.SelectSingleNode(NamespacePrefix + "Description", XmlNamespaceManager) != null ? xmlNode.SelectSingleNode(NamespacePrefix + "Description", XmlNamespaceManager).InnerText : null;
            CameraRegisterVisibility? visibilty = xmlNode.SelectSingleNode(NamespacePrefix + "Visibility", XmlNamespaceManager) != null ? (CameraRegisterVisibility?)Enum.Parse(typeof(CameraRegisterVisibility), xmlNode.SelectSingleNode(NamespacePrefix + "Visibility", XmlNamespaceManager).InnerText) : null;
            bool isStreamable = xmlNode.SelectSingleNode(NamespacePrefix + "Streamable", XmlNamespaceManager) != null ? true : false;

            return new CameraRegisterContainer(registerName, description, visibilty, isStreamable, cameraRegisterType, cameraRegisterTypeValue);
        }

        #endregion Helpers
    }
}