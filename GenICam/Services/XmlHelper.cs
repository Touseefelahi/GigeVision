using System;
using System.Collections.Generic;
using System.Xml;

namespace GenICam
{
    /// <summary>
    /// this class helps Gvcp to read all the registers from XML file
    /// </summary>
    public class XmlHelper : IXmlHelper
    {
        #region XML Setup

        private string NamespaceName { get; set; } = "ns";
        private string NamespacePrefix { get; set; } = string.Empty;
        private XmlNamespaceManager XmlNamespaceManager { get; set; } = null;
        private XmlDocument XmlDocument { get; set; } = null;
        public IGenPort GenPort { get; }

        #endregion XML Setup

        public List<ICategory> CategoryDictionary { get; private set; }

        /// <summary>
        /// the main method to read XML file
        /// </summary>
        /// <param name="registerDictionary"> Register Dictionary </param>
        /// <param name="regisetrGroupDictionary"> Register Group Dictionary</param>
        /// <param name="tagName"> First Parent Tag Name</param>
        /// <param name="xmlDocument"> XML File </param>
        public XmlHelper(string tagName, XmlDocument xmlDocument, IGenPort genPort)
        {
            var xmlRoot = xmlDocument.FirstChild.NextSibling;
            if (xmlRoot.Attributes != null)
            {
                var xmlns = xmlRoot.Attributes["xmlns"];
                if (xmlns != null)
                {
                    XmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
                    XmlNamespaceManager.AddNamespace(NamespaceName, xmlns.Value);
                    NamespacePrefix = $"{NamespaceName}:";
                }
            }
            XmlDocument = xmlDocument;
            GenPort = genPort;
            var categoryList = XmlDocument.DocumentElement.GetElementsByTagName("Category").Item(0);

            CategoryDictionary = new List<ICategory>();

            foreach (XmlNode category in categoryList.ChildNodes)
            {
                var list = GetAllCategoryFeatures(category);
                var genCategory = new GenCategory() { GroupName = category.InnerText, CategoryProperties = GetCategoryProperties(category) };
                genCategory.PFeatures = list;
                CategoryDictionary.Add(genCategory);
            }
        }

        #region GenIcam Getters

        private ICategory GetGenCategory(XmlNode node)
        {
            ICategory genCategory = null;

            switch (node.Name)
            {
                case nameof(CategoryType.StringReg):
                    genCategory = GetStringCategory(node);
                    break;

                case nameof(CategoryType.Enumeration):
                    genCategory = GetEnumerationCategory(node);
                    break;

                case nameof(CategoryType.Command):
                    genCategory = GetCommandCategory(node);
                    break;

                case nameof(CategoryType.Integer):
                    genCategory = GetIntegerCategory(node);
                    break;

                case nameof(CategoryType.Boolean):
                    genCategory = GetBooleanCategory(node);
                    break;

                case nameof(CategoryType.Float):
                    genCategory = GetFloatCategory(node);
                    break;

                default:
                    break;
            }

            return genCategory;
        }

        private List<ICategory> GetAllCategoryFeatures(XmlNode node)
        {
            var pFeatures = new List<ICategory>();
            var category = GetGenCategory(node);

            if (category is null)
            {
                var pNode = LookForChildInsideAllParents(node, node.InnerText);

                if (pNode != null)
                    category = GetGenCategory(pNode);
                else
                    pNode = node;

                if (category is null)
                {
                    foreach (XmlNode childNode in pNode.ChildNodes)
                    {
                        pNode = LookForChildInsideAllParents(childNode, childNode.InnerText);
                        if (pNode != null)
                        {
                            category = GetGenCategory(pNode);
                            if (category is null)
                            {
                                category = new GenCategory() { GroupName = childNode.InnerText };
                                category.PFeatures = GetAllCategoryFeatures(pNode);
                            }
                        }
                        if (childNode.Name == "pFeature")
                            pFeatures.Add(category);
                    }
                }
                else
                {
                    if (pNode.Name == "pFeature")
                        pFeatures.Add(category);
                }
            }
            else
            {
                if (node.Name == "pFeature")
                    pFeatures.Add(category);
            }

            return pFeatures;
        }

        private ICategory GetFloatCategory(XmlNode xmlNode)
        {
            var categoryPropreties = GetCategoryProperties(xmlNode);

            Dictionary<string, IntSwissKnife> expressions = new Dictionary<string, IntSwissKnife>();
            IPValue pValue = null;
            double min = 0, max = 0, value = 0;
            Int64 inc = 0;
            string unit = "";
            Representation representation = Representation.PureNumber;
            XmlNode pNode;
            foreach (XmlNode node in xmlNode.ChildNodes)
            {
                switch (node.Name)
                {
                    case "Value":
                        value = double.Parse(node.InnerText);

                        break;

                    case "Min":
                        min = double.Parse(node.InnerText);
                        break;

                    case "Max":
                        max = double.Parse(node.InnerText);
                        break;

                    case "pMin":
                        pNode = ReadPNode(xmlNode.ParentNode, node.InnerText);
                        expressions.Add(node.Name, GetIntSwissKnife(pNode));
                        break;

                    case "pMax":
                        pNode = ReadPNode(xmlNode.ParentNode, node.InnerText);
                        expressions.Add(node.Name, GetIntSwissKnife(pNode));
                        break;

                    case "Inc":
                        inc = Int64.Parse(node.InnerText);
                        break;

                    case "pValue":

                        pNode = ReadPNode(xmlNode.ParentNode, node.InnerText);
                        pValue = GetRegister(pNode);
                        if (pValue is null)
                            pValue = GetIntSwissKnife(pNode);
                        break;

                    case "Representation":
                        representation = Enum.Parse<Representation>(node.InnerText);
                        break;

                    case "Unit":
                        unit = node.InnerText;
                        break;

                    default:
                        break;
                }
            }

            return new GenFloat(categoryPropreties, min, max, inc, IncMode.fixedIncrement, representation, value, unit, pValue, expressions);
        }

        private ICategory GetBooleanCategory(XmlNode xmlNode)
        {
            var categoryPropreties = GetCategoryProperties(xmlNode);

            Dictionary<string, IntSwissKnife> expressions = new Dictionary<string, IntSwissKnife>();

            IPValue pValue = null;
            if (xmlNode.SelectSingleNode(NamespacePrefix + "pValue", XmlNamespaceManager) is XmlNode pValueNode)
            {
                XmlNode pNode = ReadPNode(xmlNode.ParentNode, pValueNode.InnerText);
                pValue = GetRegister(pNode);
                if (pValue is null)
                    pValue = GetIntSwissKnife(pNode);
                //expressions.Add(pValueNode.Name, GetIntSwissKnife(pNode));
            }

            return new GenBoolean(categoryPropreties, pValue, null);
        }

        private ICategory GetEnumerationCategory(XmlNode xmlNode)
        {
            var categoryProperties = GetCategoryProperties(xmlNode);

            Dictionary<string, EnumEntry> entry = new Dictionary<string, EnumEntry>();
            var enumList = xmlNode.SelectNodes(NamespacePrefix + "EnumEntry", XmlNamespaceManager);

            foreach (XmlNode enumEntry in enumList)
            {
                IIsImplemented isImplementedValue = null;
                var isImplementedNode = enumEntry.SelectSingleNode(NamespacePrefix + "pIsImplemented", XmlNamespaceManager);
                XmlNode isImplementedExpr = null;
                if (isImplementedNode != null)
                {
                    isImplementedExpr = ReadPNode(xmlNode.ParentNode, isImplementedNode.InnerText);

                    isImplementedValue = GetRegister(isImplementedExpr);
                    if (isImplementedValue is null)
                        isImplementedValue = GetIntSwissKnife(isImplementedExpr);
                    if (isImplementedValue is null)
                        isImplementedValue = GetGenCategory(isImplementedExpr);
                }

                var entryValue = UInt32.Parse(enumEntry.SelectSingleNode(NamespacePrefix + "Value", XmlNamespaceManager).InnerText);
                entry.Add(enumEntry.Attributes["Name"].Value, new EnumEntry(entryValue, isImplementedValue));
            }

            var enumPValue = xmlNode.SelectSingleNode(NamespacePrefix + "pValue", XmlNamespaceManager);
            var enumPValueNode = ReadPNode(enumPValue.ParentNode, enumPValue.InnerText);

            IPValue pValue = GetRegister(enumPValueNode);
            if (pValue is null)
                pValue = GetIntSwissKnife(enumPValueNode);

            return new GenEnumeration(categoryProperties, entry, pValue);
        }

        private ICategory GetStringCategory(XmlNode xmlNode)
        {
            var categoryProperties = GetCategoryProperties(xmlNode);

            Int64 address = 0;
            var addressNode = xmlNode.SelectSingleNode(NamespacePrefix + "Address", XmlNamespaceManager);
            if (addressNode != null)
            {
                if (addressNode.InnerText.StartsWith("0x"))
                    address = Int64.Parse(addressNode.InnerText.Substring(2), System.Globalization.NumberStyles.HexNumber);
                else
                    address = Int64.Parse(addressNode.InnerText);
            }
            ushort length = ushort.Parse(xmlNode.SelectSingleNode(NamespacePrefix + "Length", XmlNamespaceManager).InnerText);
            GenAccessMode accessMode = (GenAccessMode)Enum.Parse(typeof(GenAccessMode), xmlNode.SelectSingleNode(NamespacePrefix + "AccessMode", XmlNamespaceManager).InnerText);

            return new GenStringReg(categoryProperties, address, length, accessMode, GenPort);
        }

        private ICategory GetIntegerCategory(XmlNode xmlNode)
        {
            var categoryPropreties = GetCategoryProperties(xmlNode);

            Int64 min = 0, max = 0, inc = 0, value = 0;
            string unit = "";
            Representation representation = Representation.PureNumber;
            XmlNode pNode;

            Dictionary<string, IntSwissKnife> expressions = new Dictionary<string, IntSwissKnife>();

            IPValue pValue = null;

            foreach (XmlNode node in xmlNode.ChildNodes)
            {
                switch (node.Name)
                {
                    case "Value":
                        value = Int64.Parse(node.InnerText);

                        break;

                    case "Min":
                        min = Int64.Parse(node.InnerText);
                        break;

                    case "Max":
                        max = Int64.Parse(node.InnerText);
                        break;

                    case "pMin":
                        pNode = ReadPNode(xmlNode.ParentNode, node.InnerText);
                        expressions.Add(node.Name, GetIntSwissKnife(pNode));
                        break;

                    case "pMax":
                        pNode = ReadPNode(xmlNode.ParentNode, node.InnerText);
                        expressions.Add(node.Name, GetIntSwissKnife(pNode));
                        break;

                    case "Inc":
                        inc = Int64.Parse(node.InnerText);
                        break;

                    case "pValue":

                        pNode = ReadPNode(xmlNode.ParentNode, node.InnerText);
                        pValue = GetRegister(pNode);
                        if (pValue is null)
                            pValue = GetIntSwissKnife(pNode);

                        break;

                    case "Representation":
                        representation = Enum.Parse<Representation>(node.InnerText);
                        break;

                    case "Unit":
                        unit = node.InnerText;
                        break;

                    default:
                        break;
                }
            }

            return new GenInteger(categoryPropreties, min, max, inc, IncMode.fixedIncrement, representation, value, unit, pValue, expressions);
        }

        private ICategory GetCommandCategory(XmlNode xmlNode)
        {
            Dictionary<string, IntSwissKnife> expressions = new Dictionary<string, IntSwissKnife>();
            IPValue pValue = null;
            var categoryProperties = GetCategoryProperties(xmlNode);

            Int64 commandValue = 0;
            var commandValueNode = xmlNode.SelectSingleNode(NamespacePrefix + "CommandValue", XmlNamespaceManager);
            if (commandValueNode != null)
                commandValue = Int64.Parse(commandValueNode.InnerText);

            var pValueNode = xmlNode.SelectSingleNode(NamespacePrefix + "pValue", XmlNamespaceManager);

            var pNode = ReadPNode(xmlNode.ParentNode, pValueNode.InnerText);

            pValue = GetRegister(pNode);
            if (pValue is null)
                pValue = GetIntSwissKnife(pNode);

            return new GenCommand(categoryProperties, commandValue, pValue, null);
        }

        private IPValue GetRegister(XmlNode node)
        {
            IPValue register = null;
            switch (node.Name)
            {
                case nameof(RegisterType.Integer):
                    register = GetGenInteger(node);
                    break;

                case nameof(RegisterType.IntReg):
                    register = GetIntReg(node);
                    break;

                case nameof(RegisterType.MaskedIntReg):
                    register = GetMaskedIntReg(node);
                    break;

                case nameof(RegisterType.FloatReg):
                    register = GetFloatReg(node);
                    break;

                default:
                    break;
            }

            return register;
        }

        private IRegister GetFloatReg(XmlNode xmlNode)
        {
            Dictionary<string, IntSwissKnife> registers = new Dictionary<string, IntSwissKnife>();

            Int64 address = 0;
            var addressNode = xmlNode.SelectSingleNode(NamespacePrefix + "Address", XmlNamespaceManager);
            if (addressNode != null)
            {
                if (addressNode.InnerText.StartsWith("0x"))
                    address = Int64.Parse(addressNode.InnerText.Substring(2), System.Globalization.NumberStyles.HexNumber);
                else
                    address = Int64.Parse(addressNode.InnerText);
            }

            ushort length = ushort.Parse(xmlNode.SelectSingleNode(NamespacePrefix + "Length", XmlNamespaceManager).InnerText);
            GenAccessMode accessMode = (GenAccessMode)Enum.Parse(typeof(GenAccessMode), xmlNode.SelectSingleNode(NamespacePrefix + "AccessMode", XmlNamespaceManager).InnerText);

            if (xmlNode.SelectSingleNode(NamespacePrefix + "pAddress", XmlNamespaceManager) is XmlNode pFeatureNode)
            {
                registers.Add(pFeatureNode.InnerText, GetIntSwissKnife(ReadPNode(xmlNode.ParentNode, pFeatureNode.InnerText)));
            }

            return new GenIntReg(address, length, accessMode, registers, GenPort);
        }

        private IPValue GetGenInteger(XmlNode xmlNode)
        {
            Int64 value = 0;

            foreach (XmlNode node in xmlNode.ChildNodes)
            {
                switch (node.Name)
                {
                    case "Value":
                        value = Int64.Parse(node.InnerText);
                        break;

                    default:
                        break;
                }
            }

            return new GenInteger(value);
        }

        private IRegister GetIntReg(XmlNode xmlNode)
        {
            Dictionary<string, IntSwissKnife> expressions = new Dictionary<string, IntSwissKnife>();

            Int64 address = 0;
            var addressNode = xmlNode.SelectSingleNode(NamespacePrefix + "Address", XmlNamespaceManager);
            if (addressNode != null)
            {
                if (addressNode.InnerText.StartsWith("0x"))
                    address = Int64.Parse(addressNode.InnerText.Substring(2), System.Globalization.NumberStyles.HexNumber);
                else
                    address = Int64.Parse(addressNode.InnerText);
            }

            ushort length = ushort.Parse(xmlNode.SelectSingleNode(NamespacePrefix + "Length", XmlNamespaceManager).InnerText);
            GenAccessMode accessMode = (GenAccessMode)Enum.Parse(typeof(GenAccessMode), xmlNode.SelectSingleNode(NamespacePrefix + "AccessMode", XmlNamespaceManager).InnerText);

            if (xmlNode.SelectSingleNode(NamespacePrefix + "pAddress", XmlNamespaceManager) is XmlNode pFeatureNode)
                expressions.Add(pFeatureNode.InnerText, GetIntSwissKnife(ReadPNode(xmlNode.ParentNode, pFeatureNode.InnerText)));

            return new GenIntReg(address, length, accessMode, expressions, GenPort);
        }

        private IRegister GetMaskedIntReg(XmlNode xmlNode)
        {
            Dictionary<string, IntSwissKnife> expressions = new Dictionary<string, IntSwissKnife>();

            Int64 address = 0;
            var addressNode = xmlNode.SelectSingleNode(NamespacePrefix + "Address", XmlNamespaceManager);

            if (addressNode != null)
            {
                if (addressNode.InnerText.StartsWith("0x"))
                    address = Int64.Parse(addressNode.InnerText.Substring(2), System.Globalization.NumberStyles.HexNumber);
                else
                    address = Int64.Parse(addressNode.InnerText);
            }

            ushort length = ushort.Parse(xmlNode.SelectSingleNode(NamespacePrefix + "Length", XmlNamespaceManager).InnerText);
            GenAccessMode accessMode = Enum.Parse<GenAccessMode>(xmlNode.SelectSingleNode(NamespacePrefix + "AccessMode", XmlNamespaceManager).InnerText);

            if (xmlNode.SelectSingleNode(NamespacePrefix + "pAddress", XmlNamespaceManager) is XmlNode pFeatureNode)
                expressions.Add(pFeatureNode.InnerText, GetIntSwissKnife(ReadPNode(xmlNode.ParentNode, pFeatureNode.InnerText)));

            return new GenMaskedIntReg(address, length, accessMode, expressions, GenPort);
        }

        private IntSwissKnife GetIntSwissKnife(XmlNode xmlNode)
        {
            if (xmlNode.Name != "IntSwissKnife" && xmlNode.Name != "SwissKnife")
                return null;

            Dictionary<string, object> pVariables = new Dictionary<string, object>();

            string formula = string.Empty;

            foreach (XmlNode node in xmlNode.ChildNodes)
            {
                //child could be either pVariable or Formula
                switch (node.Name)
                {
                    case "pVariable":
                        //pVariable could be IntSwissKnife, SwissKnife, Integer, IntReg, Float, FloatReg,

                        object pVariable = null;
                        var pNode = ReadPNode(xmlNode.ParentNode, node.InnerText);
                        pVariable = pNode.Name switch
                        {
                            "IntSwissKnife" => GetIntSwissKnife(pNode),
                            "SwissKnife" => GetGenCategory(pNode),
                            _ => GetRegister(pNode)
                        };

                        if (pVariable is null)
                            pVariable = GetGenCategory(pNode);

                        pVariables.Add(node.Attributes["Name"].Value, pVariable);
                        break;

                    case "Formula":
                        formula = node.InnerText;
                        break;

                    default:
                        break;
                }
            }
            if (pVariables.Count == 0)
            {
            }
            return new IntSwissKnife(formula, pVariables);
        }

        /// <summary>
        /// Get Category Properties such as Name, AccessMode and Visibility
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private CategoryProperties GetCategoryProperties(XmlNode xmlNode)
        {
            if (xmlNode.Name == "pFeature")
                xmlNode = LookForChildInsideAllParents(xmlNode, xmlNode.InnerText);

            GenVisibility visibilty = GenVisibility.Beginner;
            string toolTip = "", description = "";
            bool isStreamable = false;
            string name = xmlNode.Attributes["Name"].Value;

            if (xmlNode.SelectSingleNode(NamespacePrefix + "Visibility", XmlNamespaceManager) is XmlNode visibilityNode)
                visibilty = Enum.Parse<GenVisibility>(visibilityNode.InnerText);
            if (xmlNode.SelectSingleNode(NamespacePrefix + "ToolTip", XmlNamespaceManager) is XmlNode toolTipNode)
                toolTip = toolTipNode.InnerText;

            if (xmlNode.SelectSingleNode(NamespacePrefix + "Description", XmlNamespaceManager) is XmlNode descriptionNode)
                description = descriptionNode.InnerText;

            var isStreamableNode = xmlNode.SelectSingleNode(NamespacePrefix + "Streamable", XmlNamespaceManager);

            if (isStreamableNode != null)
                if (isStreamableNode.InnerText == "Yes")
                    isStreamable = true;

            string rootName = "";

            if (xmlNode.ParentNode.Attributes["Comment"] != null)
                rootName = xmlNode.ParentNode.Attributes["Comment"].Value;

            return new CategoryProperties(rootName, name, toolTip, description, visibilty, isStreamable);
        }

        #endregion GenIcam Getters

        #region XML Mapping Helpers

        private XmlNode GetNodeByAttirbuteValue(XmlNode parentNode, string tagName, string value)
        {
            return parentNode.SelectSingleNode($"{NamespacePrefix}{tagName}[@Name='{value}']", XmlNamespaceManager);
        }

        private XmlNode ReadPNode(XmlNode parentNode, string pNode)
        {
            if (GetNodeByAttirbuteValue(parentNode, "Integer", pNode) is XmlNode integerNode)
            {
                var node = LookForChildInsideAllParents(integerNode, pNode);
                return node;
            }
            else if (GetNodeByAttirbuteValue(parentNode, "IntReg", pNode) is XmlNode intRegNode)
            {
                var node = LookForChildInsideAllParents(intRegNode, pNode);
                return node;
            }
            else if (GetNodeByAttirbuteValue(parentNode, "IntSwissKnife", pNode) is XmlNode intSwissKnifeNode)
            {
                var node = LookForChildInsideAllParents(intSwissKnifeNode, pNode);
                return node;
            }
            else if (GetNodeByAttirbuteValue(parentNode, "SwissKnife", pNode) is XmlNode swissKnifeNode)
            {
                var node = LookForChildInsideAllParents(swissKnifeNode, pNode);
                return node;
            }
            else if (GetNodeByAttirbuteValue(parentNode, "Float", pNode) is XmlNode floatNode)
            {
                var node = LookForChildInsideAllParents(floatNode, pNode);
                return node;
            }
            else if (GetNodeByAttirbuteValue(parentNode, "Boolean", pNode) is XmlNode booleanNode)
            {
                var node = LookForChildInsideAllParents(booleanNode, pNode);
                return node;
            }
            else if (GetNodeByAttirbuteValue(parentNode, "MaskedIntReg", pNode) is XmlNode maskedIntRegNode)
            {
                var node = LookForChildInsideAllParents(maskedIntRegNode, pNode);
                return node;
            }
            else
            {
                if (parentNode.ParentNode != null)
                    return ReadPNode(parentNode.ParentNode, pNode);
                else
                    return LookForChildInsideAllParents(parentNode.FirstChild, pNode);
            }
        }

        private XmlNode LookForChildInsideAllParents(XmlNode xmlNode, string childName)
        {
            foreach (XmlNode parent in xmlNode.ParentNode.ChildNodes)
            {
                foreach (XmlNode child in parent.ChildNodes)
                {
                    if (child.Attributes != null)
                    {
                        if (child.Attributes["Name"] != null)
                        {
                            if (child.Attributes["Name"].Value == childName)
                                return child;
                        }
                    }
                }
            }

            if (xmlNode.ParentNode.ParentNode != null)
                return LookForChildInsideAllParents(xmlNode.ParentNode, childName);
            else
            {
                var categoryList = XmlDocument.DocumentElement.ChildNodes;
                foreach (XmlNode parent in categoryList)
                {
                    foreach (XmlNode child in parent.ChildNodes)
                    {
                        if (child.Attributes != null)
                        {
                            if (child.Attributes["Name"] != null)
                            {
                                if (child.Attributes["Name"].Value == childName)
                                    return child;
                            }
                        }
                    }
                }

                return null;
            }
        }

        #endregion XML Mapping Helpers
    }
}