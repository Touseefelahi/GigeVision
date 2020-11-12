using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Xml;

namespace GenICam
{
    /// <summary>
    /// this class helps Gvcp to read all the registers from XML file
    /// </summary>
    public class XmlHelper
    {
        #region XML Setup

        private string NamespaceName { get; set; } = "ns";
        private string NamespacePrefix { get; set; } = string.Empty;
        private XmlNamespaceManager XmlNamespaceManager { get; set; } = null;
        private XmlDocument XmlDocument { get; set; } = null;

        #endregion XML Setup

        public Dictionary<string, ICategory> CategoryDictionary;

        /// <summary>
        /// the main method to read XML file
        /// </summary>
        /// <param name="registerDictionary"> Register Dictionary </param>
        /// <param name="regisetrGroupDictionary"> Register Group Dictionary</param>
        /// <param name="tagName"> First Parent Tag Name</param>
        /// <param name="xmlDocument"> XML File </param>
        public XmlHelper(string tagName, XmlDocument xmlDocument)
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

            var categoryList = XmlDocument.DocumentElement.GetElementsByTagName("Category").Item(0);

            CategoryDictionary = new Dictionary<string, ICategory>();

            foreach (XmlNode category in categoryList.ChildNodes)
            {
                var list = GetAllCategoryFeatures(category);
                var genCategory = new GenCategory();
                genCategory.PFeatures = list;
                CategoryDictionary.Add(category.InnerText, genCategory);
            }
        }

        #region GenIcam Getters

        private ICategory GetGenCategory(XmlNode node)
        {
            ICategory genCategory = null;

            switch (node.Name)
            {
                //case "pFeature":
                //    var pNode = LookForChildInsideAllParents(node, node.InnerText);
                //    genCategory = GetGenCategory(pNode);

                //    break;

                case nameof(CategoryType.StringReg):
                    genCategory = GetStringCategory(node);
                    // CategoryDictionary.Add(genCategory.CategoryProperties.Name, genCategory);
                    break;

                case nameof(CategoryType.Enumeration):
                    genCategory = GetEnumerationCategory(node);
                    //  CategoryDictionary.Add(genCategory.CategoryProperties.Name, genCategory);
                    break;

                case nameof(CategoryType.Command):
                    genCategory = GetCommandCategory(node);
                    //  CategoryDictionary.Add(genCategory.CategoryProperties.Name, genCategory);
                    break;

                case nameof(CategoryType.Integer):
                    genCategory = GetIntegerCategory(node);
                    // CategoryDictionary.Add(genCategory.CategoryProperties.Name, genCategory);

                    break;

                case nameof(CategoryType.Boolean):
                    genCategory = GetBooleanCategory(node);
                    // CategoryDictionary.Add(genCategory.CategoryProperties.Name, genCategory);

                    break;

                case nameof(CategoryType.Float):
                    genCategory = GetFloatCategory(node);
                    //CategoryDictionary.Add(genCategory.CategoryProperties.Name, genCategory);

                    break;

                default:
                    break;
            }
            //CategoryDictionary.Add(node.InnerText, genCategory);
            return genCategory;
        }

        //public ICategory GetCategory(XmlNode node)
        //{
        //    var pFeatures = new Dictionary<string, ICategory>();

        //    if (node.Name == "pFeature")
        //        pFeatures = GetAllCategoryFeatures(node);

        //    return GetGenCategory(node);

        //    //if (category is null)
        //    //{
        //    //    foreach (XmlNode childNode in node.ChildNodes)
        //    //    {
        //    //        category = GetGenCategory(childNode);
        //    //        if (category is null)
        //    //        {
        //    //            category = new GenCategory();
        //    //            category.PFeatures = GetAllCategoryFeatures(childNode);
        //    //        }

        //    //        pFeatures.Add(childNode.InnerText, category);
        //    //    }
        //    //    //if (category is null)
        //    //    //    category = new GenCategory();
        //    //    //category.PFeatures = pFeatures;
        //    //}
        //    //return category;
        //}

        private Dictionary<string, ICategory> GetAllCategoryFeatures(XmlNode node)
        {
            var pFeatures = new Dictionary<string, ICategory>();
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
                                category = new GenCategory();
                                category.PFeatures = GetAllCategoryFeatures(pNode);
                            }
                        }

                        pFeatures.Add(childNode.InnerText, category);
                    }
                }
                else
                {
                    pFeatures.Add(node.InnerText, category);
                }
            }
            else
            {
                pFeatures.Add(node.InnerText, category);
            }

            return pFeatures;
        }

        private ICategory GetFloatCategory(XmlNode xmlNode)
        {
            var categoryPropreties = GetCategoryProperties(xmlNode);

            Dictionary<string, IPRegister> registers = new Dictionary<string, IPRegister>();

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
                        registers.Add(node.Name, GetRegister(pNode));
                        break;

                    case "pMax":
                        pNode = ReadPNode(xmlNode.ParentNode, node.InnerText);
                        registers.Add(node.Name, GetRegister(pNode));
                        break;

                    case "Inc":
                        inc = Int64.Parse(node.InnerText);
                        break;

                    case "pValue":

                        pNode = ReadPNode(xmlNode.ParentNode, node.InnerText);
                        registers.Add(node.Name, GetRegister(pNode));
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

            return new GenFloat(categoryPropreties, min, max, inc, IncMode.fixedIncrement, representation, value, unit, registers);
        }

        private ICategory GetBooleanCategory(XmlNode xmlNode)
        {
            var categoryPropreties = GetCategoryProperties(xmlNode);

            Dictionary<string, IPRegister> registers = new Dictionary<string, IPRegister>();

            if (xmlNode.SelectSingleNode(NamespacePrefix + "pValue", XmlNamespaceManager) is XmlNode pValueNode)
            {
                XmlNode pNode = ReadPNode(xmlNode.ParentNode, pValueNode.InnerText);
                registers.Add(pValueNode.Name, GetRegister(pNode));
            }

            return new GenBoolean(categoryPropreties, registers);
        }

        private ICategory GetEnumerationCategory(XmlNode xmlNode)
        {
            var categoryProperties = GetCategoryProperties(xmlNode);

            Dictionary<string, EnumEntry> entry = new Dictionary<string, EnumEntry>();
            var enumList = xmlNode.SelectNodes(NamespacePrefix + "EnumEntry", XmlNamespaceManager);

            foreach (XmlNode enumEntry in enumList)
            {
                Dictionary<string, IPRegister> enumPFeatures = new Dictionary<string, IPRegister>();

                var isImplementedNode = enumEntry.SelectSingleNode(NamespacePrefix + "pIsImplemented", XmlNamespaceManager);
                XmlNode isImplementedExpr = null;
                if (isImplementedNode != null)
                {
                    isImplementedExpr = ReadPNode(xmlNode.ParentNode, isImplementedNode.InnerText);
                    enumPFeatures.Add(isImplementedNode.Name, GetRegister(isImplementedExpr));
                }

                var entryValue = UInt32.Parse(enumEntry.SelectSingleNode(NamespacePrefix + "Value", XmlNamespaceManager).InnerText);
                entry.Add(enumEntry.Attributes["Name"].Value, new EnumEntry(entryValue, enumPFeatures));
            }

            var enumPValue = xmlNode.SelectSingleNode(NamespacePrefix + "pValue", XmlNamespaceManager);
            var enumPValueNode = ReadPNode(enumPValue.ParentNode, enumPValue.InnerText);

            Dictionary<string, IPRegister> registers = new Dictionary<string, IPRegister>();
            var pFeature = GetRegister(enumPValueNode);
            registers.Add(enumPValue.Name, pFeature);
            return new GenEnumeration(categoryProperties, entry, registers);
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

            return new GenStringReg(categoryProperties, address, length, accessMode);
        }

        private ICategory GetIntegerCategory(XmlNode xmlNode)
        {
            var categoryPropreties = GetCategoryProperties(xmlNode);
            if (categoryPropreties is null)
            {
            }
            Dictionary<string, IPRegister> registers = new Dictionary<string, IPRegister>();
            Int64 min = 0, max = 0, inc = 0, value = 0;
            string unit = "";
            Representation representation = Representation.PureNumber;
            XmlNode pNode;
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
                        registers.Add(node.Name, GetRegister(pNode));
                        break;

                    case "pMax":
                        pNode = ReadPNode(xmlNode.ParentNode, node.InnerText);
                        registers.Add(node.Name, GetRegister(pNode));
                        break;

                    case "Inc":
                        inc = Int64.Parse(node.InnerText);
                        break;

                    case "pValue":

                        pNode = ReadPNode(xmlNode.ParentNode, node.InnerText);
                        registers.Add(node.Name, GetRegister(pNode));
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

            return new GenInteger(categoryPropreties, min, max, inc, IncMode.fixedIncrement, representation, value, unit, registers);
        }

        private ICategory GetCommandCategory(XmlNode xmlNode)
        {
            Dictionary<string, IPRegister> registers = new Dictionary<string, IPRegister>();
            var categoryProperties = GetCategoryProperties(xmlNode);

            Int64 commandValue = 0;
            var commandValueNode = xmlNode.SelectSingleNode(NamespacePrefix + "CommandValue", XmlNamespaceManager);
            if (commandValueNode != null)
                commandValue = Int64.Parse(commandValueNode.InnerText);

            var pValue = xmlNode.SelectSingleNode(NamespacePrefix + "pValue", XmlNamespaceManager);
            registers.Add(pValue.Name, GetRegister(ReadPNode(xmlNode.ParentNode, pValue.InnerText)));

            return new GenCommand(categoryProperties, commandValue, registers);
        }

        private IPRegister GetRegister(XmlNode node)
        {
            IPRegister register = null;
            if (node is null)
            {
            }
            switch (node.Name)
            {
                case nameof(RegisterType.Integer):
                    register = GetGenInteger(node) as IPRegister;
                    break;

                case nameof(RegisterType.IntReg):
                    register = GetIntReg(node);
                    break;

                case nameof(RegisterType.MaskedIntReg):
                    register = GetMaskedIntReg(node);
                    break;

                case nameof(RegisterType.IntSwissKnife):
                    register = GetIntSwissKnife(node);
                    break;

                case nameof(RegisterType.Float):
                    register = GetGenFloat(node);
                    break;

                case nameof(RegisterType.FloatReg):
                    register = GetFloatReg(node);
                    break;

                default:
                    break;
            }

            return register;
        }

        private IPRegister GetFloatReg(XmlNode xmlNode)
        {
            Dictionary<string, IPRegister> registers = new Dictionary<string, IPRegister>();

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
                registers.Add(pFeatureNode.InnerText, GetRegister(ReadPNode(xmlNode.ParentNode, pFeatureNode.InnerText)));
            }

            return new GenIntReg(address, length, accessMode, registers);
        }

        private IPRegister GetGenFloat(XmlNode node)
        {
            throw new NotImplementedException();
        }

        private IGenCategory GetGenInteger(XmlNode xmlNode)
        {
            Dictionary<string, IPRegister> registers = new Dictionary<string, IPRegister>();

            Int64 min, max, inc, value = 0;
            string integerPNode;

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
                        registers.Add(node.Name, GetRegister(ReadPNode(xmlNode, node.InnerText)));
                        break;

                    case "pMax":
                        registers.Add(node.Name, GetRegister(ReadPNode(xmlNode, node.InnerText)));
                        break;

                    case "Inc":
                        inc = Int64.Parse(node.InnerText);
                        break;

                    case "pValue":
                        registers.Add(node.Name, GetRegister(ReadPNode(xmlNode, node.InnerText)));
                        break;

                    default:
                        break;
                }
            }

            return new GenInteger(value);
        }

        private IPRegister GetIntReg(XmlNode xmlNode)
        {
            Dictionary<string, IPRegister> registers = new Dictionary<string, IPRegister>();

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
                registers.Add(pFeatureNode.InnerText, GetRegister(ReadPNode(xmlNode.ParentNode, pFeatureNode.InnerText)));

            return new GenIntReg(address, length, accessMode, registers);
        }

        private IPRegister GetMaskedIntReg(XmlNode xmlNode)
        {
            Dictionary<string, IPRegister> registers = new Dictionary<string, IPRegister>();

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
                registers.Add(pFeatureNode.InnerText, GetRegister(ReadPNode(xmlNode.ParentNode, pFeatureNode.InnerText)));

            return new GenMaskedIntReg(address, length, accessMode, registers);
        }

        private IPRegister GetIntSwissKnife(XmlNode xmlNode)
        {
            Dictionary<string, IPRegister> pVariables = new Dictionary<string, IPRegister>();

            string formula = string.Empty;

            foreach (XmlNode node in xmlNode.ChildNodes)
            {
                //child could be either pVariable or Formula
                switch (node.Name)
                {
                    case "pVariable":
                        //pVariable could be IntSwissKnife, SwissKnife, Integer, IntReg, Float, FloatReg,
                        pVariables.Add(node.InnerText, GetRegister(ReadPNode(xmlNode.ParentNode, node.InnerText)));
                        break;

                    case "Formula":
                        formula = node.InnerText;
                        break;

                    default:
                        break;
                }
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