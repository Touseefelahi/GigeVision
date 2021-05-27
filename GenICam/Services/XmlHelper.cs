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

        public XmlNode Xmlns { get; private set; }

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
                Xmlns = xmlRoot.Attributes["xmlns"];
                if (Xmlns != null)
                {
                    XmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
                    XmlNamespaceManager.AddNamespace(NamespaceName, Xmlns.Value);
                    NamespacePrefix = $"{NamespaceName}:";
                }
            }
            XmlDocument = xmlDocument;
            GenPort = genPort;
            var categoryList = XmlDocument.DocumentElement.GetElementsByTagName("Category").Item(0);

            CategoryDictionary = GetCategoryFeatures(categoryList);

            TempDictionary.Clear();
        }

        private List<ICategory> GetCategoryFeatures(XmlNode categoryList)
        {
            var categoryDictionary = new List<ICategory>();

            foreach (XmlNode category in categoryList.ChildNodes)
            {
                var list = ReadCategoryFeature(category);
                if (list.Count < 1)
                    continue;
                var genCategory = new GenCategory() { GroupName = category.InnerText, CategoryProperties = GetCategoryProperties(category) };
                genCategory.PFeatures = list;
                categoryDictionary.Add(genCategory);
            }
            return categoryDictionary;
        }

        private List<ICategory> GetSubCategoryFeatures(XmlNode categoryList)
        {
            var categoryDictionary = new List<ICategory>();

            foreach (XmlNode category in categoryList.ChildNodes)
            {
                var list = ReadCategoryFeature(category);
                if (list.Count < 1)
                    continue;
                foreach (var genCategory in list)
                {
                    categoryDictionary.Add(genCategory);
                }
            }
            return categoryDictionary;
        }

        #region GenIcam Getters

        private List<ICategory> ReadCategoryFeature(XmlNode node)
        {
            var pFeatures = new List<ICategory>();
            ICategory category = null;
            if (node.Name != "pFeature")
                return pFeatures;

            var pNode = LookForChildInsideAllParents(node, node.InnerText);

            if (pNode != null)
            {
                if (pNode.Name == "Category")
                {
                    category = new GenCategory() { GroupName = node.InnerText, CategoryProperties = GetCategoryProperties(node) };
                    category.PFeatures = GetSubCategoryFeatures(pNode);
                }
                else
                {
                    category = GetGenCategory(pNode);
                }
                pFeatures.Add(category);
            }
            return pFeatures;
        }

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

        private ICategory GetFloatCategory(XmlNode xmlNode)
        {
            var categoryPropreties = GetCategoryProperties(xmlNode);

            Dictionary<string, IMathematical> expressions = new Dictionary<string, IMathematical>();
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
                        double.TryParse(node.InnerText, out value);

                        break;

                    case "Min":
                        double.TryParse(node.InnerText, out min);
                        break;

                    case "Max":
                        double.TryParse(node.InnerText, out max);
                        break;

                    case "pMin":
                        pNode = ReadPNode(xmlNode.ParentNode, node.InnerText);
                        if (pNode != null)
                            expressions.Add(node.Name, GetFormula(pNode));

                        break;

                    case "pMax":
                        pNode = ReadPNode(xmlNode.ParentNode, node.InnerText);
                        if (pNode != null)
                            expressions.Add(node.Name, GetFormula(pNode));
                        break;

                    case "Inc":
                        Int64.TryParse(node.InnerText, out inc);
                        break;

                    case "pValue":

                        pNode = ReadPNode(xmlNode.ParentNode, node.InnerText);
                        if (pNode != null)
                        {
                            pValue = GetRegister(pNode);
                            if (pValue is null)
                                pValue = GetFormula(pNode);
                        }
                        break;

                    case "Representation":
                        Enum.TryParse<Representation>(node.InnerText, out representation);
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

            Dictionary<string, IMathematical> expressions = new Dictionary<string, IMathematical>();

            IPValue pValue = null;
            if (xmlNode.SelectSingleNode(NamespacePrefix + "pValue", XmlNamespaceManager) is XmlNode pValueNode)
            {
                XmlNode pNode = ReadPNode(xmlNode.ParentNode, pValueNode.InnerText);
                if (pNode != null)
                {
                    pValue = GetRegister(pNode);
                    if (pValue is null)
                        pValue = GetFormula(pNode);
                }
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
                    if (isImplementedExpr != null)
                    {
                        isImplementedValue = GetRegister(isImplementedExpr);
                        if (isImplementedValue is null)
                            isImplementedValue = GetFormula(isImplementedExpr);
                        if (isImplementedValue is null)
                            isImplementedValue = GetGenCategory(isImplementedExpr);
                    }
                }
                uint entryValue;
                UInt32.TryParse(enumEntry.SelectSingleNode(NamespacePrefix + "Value", XmlNamespaceManager).InnerText, out entryValue);
                entry.Add(enumEntry.Attributes["Name"].Value, new EnumEntry(entryValue, isImplementedValue));
            }
            IPValue pValue = null;

            var enumPValue = xmlNode.SelectSingleNode(NamespacePrefix + "pValue", XmlNamespaceManager);
            if (enumPValue != null)
            {
                var enumPValueNode = ReadPNode(enumPValue.ParentNode, enumPValue.InnerText);
                if (enumPValueNode != null)
                {
                    pValue = GetRegister(enumPValueNode);
                    if (pValue is null)
                        pValue = GetFormula(enumPValueNode);
                }
            }

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

            Dictionary<string, IMathematical> expressions = new Dictionary<string, IMathematical>();

            IPValue pValue = null;

            foreach (XmlNode node in xmlNode.ChildNodes)
            {
                switch (node.Name)
                {
                    case "Value":
                        Int64.TryParse(node.InnerText, out value);

                        break;

                    case "Min":
                        Int64.TryParse(node.InnerText, out min);
                        break;

                    case "Max":
                        Int64.TryParse(node.InnerText, out max);
                        break;

                    case "pMin":
                        pNode = ReadPNode(xmlNode.ParentNode, node.InnerText);
                        if (pNode != null)
                            expressions.Add(node.Name, GetFormula(pNode));

                        break;

                    case "pMax":
                        pNode = ReadPNode(xmlNode.ParentNode, node.InnerText);
                        if (pNode != null)
                            expressions.Add(node.Name, GetFormula(pNode));

                        break;

                    case "Inc":
                        Int64.TryParse(node.InnerText, out inc);

                        break;

                    case "pValue":

                        pNode = ReadPNode(xmlNode.ParentNode, node.InnerText);
                        if (pNode != null)
                        {
                            pValue = GetRegister(pNode);
                            if (pValue is null)
                                pValue = GetFormula(pNode);
                        }

                        break;

                    case "Representation":
                        Enum.TryParse<Representation>(node.InnerText, out representation);
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
            Dictionary<string, IMathematical> expressions = new Dictionary<string, IMathematical>();
            IPValue pValue = null;
            var categoryProperties = GetCategoryProperties(xmlNode);

            Int64 commandValue = 0;
            var commandValueNode = xmlNode.SelectSingleNode(NamespacePrefix + "CommandValue", XmlNamespaceManager);
            if (commandValueNode != null)
                Int64.TryParse(commandValueNode.InnerText, out commandValue);

            var pValueNode = xmlNode.SelectSingleNode(NamespacePrefix + "pValue", XmlNamespaceManager);

            var pNode = ReadPNode(xmlNode.ParentNode, pValueNode.InnerText);

            if (pNode != null)
            {
                pValue = GetRegister(pNode);
                if (pValue is null)
                    pValue = GetFormula(pNode);
            }

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
            Dictionary<string, IMathematical> registers = new Dictionary<string, IMathematical>();

            Int64? address = null;
            object pAddress = null;
            var addressNode = xmlNode.SelectSingleNode(NamespacePrefix + "Address", XmlNamespaceManager);

            if (addressNode != null)
            {
                if (addressNode.InnerText.StartsWith("0x"))
                    address = Int64.Parse(addressNode.InnerText.Substring(2), System.Globalization.NumberStyles.HexNumber);
                else
                    address = Int64.Parse(addressNode.InnerText);
            }

            if (xmlNode.SelectSingleNode(NamespacePrefix + "pAddress", XmlNamespaceManager) is XmlNode pFeatureNode)
            {
                var pNode = ReadPNode(xmlNode.ParentNode, pFeatureNode.InnerText);

                if (pNode != null)
                {
                    switch (pNode.Name)
                    {
                        case "IntSwissKnife":
                        case "SwissKnife":
                            pAddress = GetIntSwissKnife(pNode);
                            break;

                        case "IntConverter":
                        case "Converter":
                            pAddress = GetConverter(pNode);
                            break;

                        default:
                            pAddress = GetRegister(pNode);
                            if (pAddress is null)
                                pAddress = GetGenCategory(pNode);
                            break;
                    }

                    if (pAddress is null)
                        pAddress = GetGenCategory(pNode);
                }

            }


            ushort length = ushort.Parse(xmlNode.SelectSingleNode(NamespacePrefix + "Length", XmlNamespaceManager).InnerText);
            GenAccessMode accessMode = (GenAccessMode)Enum.Parse(typeof(GenAccessMode), xmlNode.SelectSingleNode(NamespacePrefix + "AccessMode", XmlNamespaceManager).InnerText);


            return new GenIntReg(address, length, accessMode, registers, pAddress, GenPort);
        }

        private IPValue GetGenInteger(XmlNode xmlNode)
        {
            Int64 value = 0;
            IPValue pValue = null;
            foreach (XmlNode node in xmlNode.ChildNodes)
            {
                switch (node.Name)
                {
                    case "Value":
                        if (node.InnerText.StartsWith("0x"))
                            value = Int64.Parse(node.InnerText.Substring(2), System.Globalization.NumberStyles.HexNumber);
                        else
                            Int64.TryParse(node.InnerText, out value);
                        break;

                    case "pValue":
                        var pValueNode = ReadPNode(xmlNode.ParentNode, node.InnerText);
                        if (pValueNode != null)
                        {
                            pValue = GetRegister(pValueNode);
                            if (pValue is null)
                                pValue = GetIntSwissKnife(pValueNode);
                            if (pValue is null)
                                pValue = GetConverter(pValueNode);
                        }
                        break;

                    default:
                        break;
                }
            }

            return new GenInteger(value, pValue);
        }

        private IRegister GetIntReg(XmlNode xmlNode)
        {
            Dictionary<string, IMathematical> expressions = new Dictionary<string, IMathematical>();

            Int64? address = null;
            object pAddress = null;
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
                var pNode = ReadPNode(xmlNode.ParentNode, pFeatureNode.InnerText);

                if (pNode != null)
                {
                    switch (pNode.Name)
                    {
                        case "IntSwissKnife":
                        case "SwissKnife":
                            pAddress = GetIntSwissKnife(pNode);
                            break;

                        case "IntConverter":
                        case "Converter":
                            pAddress = GetConverter(pNode);
                            break;

                        default:
                            pAddress = GetRegister(pNode);
                            if (pAddress is null)
                                pAddress = GetGenCategory(pNode);
                            break;
                    }

                    if (pAddress is null)
                        pAddress = GetGenCategory(pNode);
                }

            }


            return new GenIntReg(address, length, accessMode, expressions, pAddress, GenPort);
        }

        private GenMaskedIntReg GetMaskedIntReg(XmlNode xmlNode)
        {
            Int64? address = null;
            object pAddress = null;
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
            {
                var pNode = ReadPNode(xmlNode.ParentNode, pFeatureNode.InnerText);

                if (pNode != null)
                {
                    switch (pNode.Name)
                    {
                        case "IntSwissKnife":
                        case "SwissKnife":
                            pAddress = GetIntSwissKnife(pNode);
                            break;

                        case "IntConverter":
                        case "Converter":
                            pAddress = GetConverter(pNode);
                            break;

                        default:
                            pAddress = GetRegister(pNode);
                            if (pAddress is null)
                                pAddress = GetGenCategory(pNode);
                            break;
                    }

                    if (pAddress is null)
                        pAddress = GetGenCategory(pNode);
                }

            }


            short? lsb = null, msb = null;
            byte? bit = null;
            var lsbNode = xmlNode.SelectSingleNode(NamespacePrefix + "LSB", XmlNamespaceManager);
            var msbNode = xmlNode.SelectSingleNode(NamespacePrefix + "MSB", XmlNamespaceManager);
            var bitNode = xmlNode.SelectSingleNode(NamespacePrefix + "Bit", XmlNamespaceManager);

            if (lsbNode != null)
                lsb = short.Parse(lsbNode.InnerText);
            if (msbNode != null)
                msb = short.Parse(msbNode.InnerText);
            if (bitNode != null)
                bit = byte.Parse(bitNode.InnerText);
            return new GenMaskedIntReg(address, length, msb, lsb, bit, accessMode, pAddress, GenPort);
        }

        public IMathematical GetFormula(XmlNode xmlNode)
        {
            if (xmlNode.Name == "IntConverter" || xmlNode.Name == "Converter")
                return GetConverter(xmlNode);

            if (xmlNode.Name == "IntSwissKnife" || xmlNode.Name != "SwissKnife")
                return GetIntSwissKnife(xmlNode);

            return null;
        }

        private IntSwissKnife GetIntSwissKnife(XmlNode xmlNode)
        {
            if (xmlNode.Name != "IntSwissKnife" && xmlNode.Name != "SwissKnife")
                return null;

            Dictionary<string, IPValue> pVariables = new Dictionary<string, IPValue>();
            Dictionary<string, double> constants = new Dictionary<string, double>();
            Dictionary<string, string> expressions = new Dictionary<string, string>();

            string formula = string.Empty;

            foreach (XmlNode node in xmlNode.ChildNodes)
            {
                //child could be either pVariable or Formula
                switch (node.Name)
                {
                    case "pVariable":
                        //pVariable could be IntSwissKnife, SwissKnife, Integer, IntReg, Float, FloatReg,

                        IPValue pVariable = null;
                        var pNode = ReadPNode(xmlNode.ParentNode, node.InnerText);
                        if (pNode != null)
                        {
                            switch (pNode.Name)
                            {
                                case "IntSwissKnife":
                                case "SwissKnife":
                                    pVariable = GetIntSwissKnife(pNode);
                                    break;

                                case "IntConverter":
                                case "Converter":
                                    pVariable = GetConverter(pNode);
                                    break;

                                default:
                                    pVariable = GetRegister(pNode);
                                    if (pVariable is null)
                                        pVariable = GetGenCategory(pNode) as IPValue;
                                    break;
                            }

                            if (pVariable is null)
                                pVariable = GetGenCategory(pNode) as IPValue;

                            pVariables.Add(node.Attributes["Name"].Value, pVariable);
                        }
                        break;

                    case "Constant":
                        constants.Add(node.Attributes["Name"].Value, double.Parse(node.InnerText));
                        break;

                    case "Expression":
                        expressions.Add(node.Attributes["Name"].Value, node.InnerText);
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
            return new IntSwissKnife(formula, pVariables, constants, expressions);
        }

        private Converter GetConverter(XmlNode xmlNode)
        {
            if (xmlNode.Name != "IntConverter" && xmlNode.Name != "Converter")
                return null;

            Dictionary<string, IPValue> pVariables = new Dictionary<string, IPValue>();

            string formulaFrom = string.Empty;
            string formulaTo = string.Empty;
            IPValue pValue = null;
            Slope slope = Slope.None;

            foreach (XmlNode node in xmlNode.ChildNodes)
            {
                //child could be either pVariable or Formula
                switch (node.Name)
                {
                    case "pVariable":
                        //pVariable could be IntSwissKnife, SwissKnife, Integer, IntReg, Float, FloatReg,

                        IPValue pVariable = null;
                        var pNode = ReadPNode(xmlNode.ParentNode, node.InnerText);
                        if (pNode != null)
                        {
                            switch (pNode.Name)
                            {
                                case "IntSwissKnife":
                                case "SwissKnife":
                                    pVariable = GetIntSwissKnife(pNode);
                                    break;

                                case "IntConverter":
                                case "Converter":
                                    pVariable = GetConverter(pNode);
                                    break;

                                default:
                                    pVariable = GetRegister(pNode);
                                    if (pVariable is null)
                                        pVariable = GetGenCategory(pNode) as IPValue;
                                    break;
                            }

                            if (pVariable is null)
                                pVariable = GetGenCategory(pNode) as IPValue;

                            pVariables.Add(node.Attributes["Name"].Value, pVariable);
                        }
                        break;

                    case "FormulaTo":
                        formulaTo = node.InnerText;
                        break;

                    case "FormulaFrom":
                        formulaFrom = node.InnerText;
                        break;

                    case "pValue":
                        var pValueNode = ReadPNode(node.ParentNode, node.InnerText);
                        if (pValueNode != null)
                        {
                            pValue = GetRegister(pValueNode);
                            if (pValue is null)
                                pValue = GetIntSwissKnife(pValueNode);
                            if (pValue is null)
                                pValue = GetConverter(pValueNode);
                            if (pValue is null)
                                pValue = GetGenCategory(pValueNode) as IPValue;
                        }
                        break;

                    case "Slope":
                        slope = Enum.Parse<Slope>(node.InnerText);

                        break;

                    default:
                        break;
                }
            }
            if (pVariables.Count == 0)
            {
            }
            return new Converter(formulaTo, formulaFrom, pValue, slope, pVariables);
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
            string displayName = "", toolTip = "", description = "";
            bool isStreamable = false;

            //if (xmlNode.SelectSingleNode(NamespacePrefix + "DisplayName", XmlNamespaceManager) is XmlNode displayNameNode)
            //    displayName = displayNameNode.InnerText;

            if (displayName == "")
            {
                displayName = xmlNode.Attributes["Name"].Value;
            }

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

            return new CategoryProperties(rootName, displayName, toolTip, description, visibilty, isStreamable);
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