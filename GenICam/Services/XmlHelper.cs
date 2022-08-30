using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace GenICam
{
    /// <summary>
    /// this class helps Gvcp to read all the registers from XML file
    /// </summary>
    public class XmlHelper : IXmlHelper
    {
        private XmlNode xmlRoot;
        #region XML Setup
        private string namespacePrefix;
        private XmlNamespaceManager xmlNamespaceManager;
        private XmlDocument xmlDocument;
        public IPort GenPort { get; }
        #endregion XML Setup

        public List<ICategory> CategoryDictionary { get; private set; }

        private XmlNode rootCategoryNode;

        /// <summary>
        /// the main method to read XML file
        /// </summary>
        /// <param name="registerDictionary"> Register Dictionary </param>
        /// <param name="regisetrGroupDictionary"> Register Group Dictionary</param>
        /// <param name="rootElementName"> rootElement Name</param>
        /// <param name="xmlDocument"> XML File </param>
        public XmlHelper(XmlDocument xmlDocument, IPort genPort)
        {
            xmlRoot = xmlDocument["RegisterDescription"];
            if (xmlRoot.Attributes != null)
            {
                var uri = xmlRoot.Attributes["xmlns"].Value;
                namespacePrefix = xmlRoot.Attributes["StandardNameSpace"].Value;

                xmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
                xmlNamespaceManager.AddNamespace(namespacePrefix, uri);
            }
            this.xmlDocument = xmlDocument;
            GenPort = genPort;

        }

        /// <summary>
        /// The nodes can either be retrieved by their(unique) name or can be found by traversing the node graph starting with the root node.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> LoadUp(bool isReadAllRegisters = false)
        {
            CategoryDictionary = new List<ICategory>();
            rootCategoryNode = xmlRoot.SelectSingleNode("//" + namespacePrefix + ":Category[@Name=\"Root\"]", xmlNamespaceManager);
            if (isReadAllRegisters)
            {
                if ((await GetCategoryFeatures(rootCategoryNode)) is ICategory category)
                {
                    CategoryDictionary.Add(category);
                }

                return CategoryDictionary.Count > 0;
            }

            return rootCategoryNode != null && !isReadAllRegisters;
        }

        private async Task<ICategory> GetCategoryFeatures(XmlNode categoryNode)
        {
            string groupName = categoryNode.Attributes["Name"].Value;
            var genCategory = new GenCategory() { GroupName = groupName, CategoryProperties = await GetCategoryProperties(categoryNode) };

            genCategory.PFeatures = await ReadCategoryFeature(categoryNode);
            return genCategory;
        }

        #region GenIcam Getters
        private async Task<List<ICategory>> ReadCategoryFeature(XmlNode node)
        {
            var pFeatures = new List<ICategory>();
            ICategory category;
            var pFeatureNodes = SelectNodes(node, "pFeature");
            foreach (XmlNode pFeatureNode in pFeatureNodes)
            {
                XmlNodeList pNodes = GetAllNodesByAttirbuteValue(attirbuteValue: pFeatureNode.InnerText, attirbuteName: "Name");
                foreach (XmlNode pNode in pNodes)
                {
                    if (pNode != null)
                    {
                        category = await GetGenCategory(pNode);
                        pFeatures.Add(category);
                    }
                }
            }

            return pFeatures;
        }
        private async Task<ICategory> GetGenCategory(XmlNode node)
        {
            ICategory genCategory = null;
            switch (node.Name)
            {
                case nameof(CategoryType.StringReg):
                    genCategory = await GetStringCategory(node);
                    break;

                case nameof(CategoryType.Enumeration):
                    genCategory = await GetEnumerationCategory(node);
                    break;

                case nameof(CategoryType.Command):
                    genCategory = await GetCommandCategory(node);
                    break;

                case nameof(CategoryType.Integer):
                    genCategory = await GetIntegerCategory(node);
                    break;

                case nameof(CategoryType.Boolean):
                    genCategory = await GetBooleanCategory(node);
                    break;

                case nameof(CategoryType.Float):
                    genCategory = await GetFloatCategory(node);
                    break;

                case nameof(CategoryType.Category):
                    genCategory = await GetCategoryFeatures(node);
                    break;
                default:
                    break;
            }

            return genCategory;
        }
        private async Task<ICategory> GetFloatCategory(XmlNode xmlNode)
        {
            var categoryPropreties = await GetCategoryProperties(xmlNode);

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
                        pNode = await ReadPNode(node.InnerText);
                        if (pNode != null)
                            expressions.Add(node.Name, await GetFormula(pNode));

                        break;

                    case "pMax":
                        pNode = await ReadPNode(node.InnerText);
                        if (pNode != null)
                            expressions.Add(node.Name, await GetFormula(pNode));
                        break;

                    case "Inc":
                        Int64.TryParse(node.InnerText, out inc);
                        break;

                    case "pValue":

                        pNode = await ReadPNode(node.InnerText);
                        if (pNode != null)
                        {
                            pValue = await GetRegister(pNode);
                            if (pValue is null)
                                pValue = await GetFormula(pNode);
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

            return new GenFloat(categoryPropreties, min, max, inc, IncrementMode.fixedIncrement, representation, value, unit, pValue, expressions);
        }
        private async Task<ICategory> GetBooleanCategory(XmlNode xmlNode)
        {
            var categoryPropreties = await GetCategoryProperties(xmlNode);

            Dictionary<string, IMathematical> expressions = new Dictionary<string, IMathematical>();

            IPValue pValue = null;
            if (SelectSingleNode(xmlNode, "pValue") is XmlNode pValueNode)
            {
                XmlNode pNode = await ReadPNode(pValueNode.InnerText);
                if (pNode != null)
                {
                    pValue = await GetRegister(pNode);
                    if (pValue is null)
                        pValue = await GetFormula(pNode);
                }
                //expressions.Add(pValueNode.Name, GetIntSwissKnife(pNode));
            }

            return new GenBoolean(categoryPropreties, pValue, null);
        }
        private async Task<ICategory> GetEnumerationCategory(XmlNode xmlNode)
        {
            var categoryProperties = await GetCategoryProperties(xmlNode);

            Dictionary<string, EnumEntry> entry = new Dictionary<string, EnumEntry>();
            XmlNodeList enumList = SelectNodes(xmlNode, "EnumEntry");
            foreach (XmlNode enumEntry in enumList)
            {
                IIsImplemented isImplementedValue = null;
                //XmlNode isImplementedNode = SelectSingleNode(enumEntry, "pIsImplemented");
                //XmlNode isImplementedExpr = null;
                //if (isImplementedNode != null)
                //{
                //    isImplementedExpr = await ReadPNode(isImplementedNode.InnerText);
                //    if (isImplementedExpr != null)
                //    {
                //        isImplementedValue = await GetRegister(isImplementedExpr);
                //        if (isImplementedValue is null)
                //            isImplementedValue = await GetFormula(isImplementedExpr);
                //        if (isImplementedValue is null)
                //            isImplementedValue = await GetGenCategory(isImplementedExpr);
                //    }
                //}
                uint entryValue;
                UInt32.TryParse(SelectSingleNode(enumEntry, "Value").InnerText, out entryValue);
                entry.Add(enumEntry.Attributes["Name"].Value, new EnumEntry(entryValue, isImplementedValue));
            }
            IPValue pValue = null;

            var enumPValue = SelectSingleNode(xmlNode, "pValue");
            if (enumPValue != null)
            {
                var enumPValueNode = await ReadPNode(enumPValue.InnerText);
                if (enumPValueNode != null)
                {
                    pValue = await GetRegister(enumPValueNode);
                    if (pValue is null)
                        pValue = await GetFormula(enumPValueNode);
                }
            }

            return new GenEnumeration(categoryProperties, entry, pValue);
        }
        private async Task<ICategory> GetStringCategory(XmlNode xmlNode)
        {
            var categoryProperties = await GetCategoryProperties(xmlNode);

            Int64 address = 0;
            var addressNode = SelectSingleNode(xmlNode, "Address");
            if (addressNode != null)
            {
                if (addressNode.InnerText.StartsWith("0x"))
                    address = Int64.Parse(addressNode.InnerText.Substring(2), System.Globalization.NumberStyles.HexNumber);
                else
                    address = Int64.Parse(addressNode.InnerText);
            }
            ushort length = ushort.Parse(SelectSingleNode(xmlNode, "Length").InnerText);
            GenAccessMode accessMode = (GenAccessMode)Enum.Parse(typeof(GenAccessMode), SelectSingleNode(xmlNode, "AccessMode").InnerText);

            return new GenStringReg(categoryProperties, address, length, accessMode, GenPort);
        }
        private async Task<ICategory> GetIntegerCategory(XmlNode xmlNode)
        {
            var categoryPropreties = await GetCategoryProperties(xmlNode);

            Int64? min = 0, max = 0, inc = 0, value = 0;
            string unit = "";
            Representation representation = Representation.PureNumber;
            XmlNode pNode;

            Dictionary<string, IMathematical> expressions = new Dictionary<string, IMathematical>();

            IPValue pValue = null;


            var representationNode = SelectSingleNode(xmlNode, "Representation");
            if (representationNode != null)
            {
                Enum.TryParse<Representation>(representationNode.InnerText, out representation);
            }

            int convertFromBase = 10;
            if (representation == Representation.HexNumber)
            {
                convertFromBase = 16;
            }


            foreach (XmlNode node in xmlNode.ChildNodes)
            {
                switch (node.Name)
                {
                    case "Value":
                        value = Convert.ToInt64(node.InnerText, convertFromBase);

                        break;

                    case "Min":
                        min = Convert.ToInt64(node.InnerText, convertFromBase);
                        break;

                    case "Max":
                        max = Convert.ToInt64(node.InnerText, convertFromBase);
                        break;

                    case "pMin":
                        pNode = await ReadPNode(node.InnerText);
                        if (pNode != null)
                            expressions.Add(node.Name, await GetFormula(pNode));

                        break;

                    case "pMax":
                        pNode = await ReadPNode(node.InnerText);
                        if (pNode != null)
                            expressions.Add(node.Name, await GetFormula(pNode));

                        break;

                    case "Inc":
                        inc = Convert.ToInt64(node.InnerText, convertFromBase);

                        break;

                    case "pValue":

                        pNode = await ReadPNode(node.InnerText);
                        if (pNode != null)
                        {
                            pValue = await GetRegister(pNode);
                            if (pValue is null)
                                pValue = await GetFormula(pNode);
                        }

                        break;

                    case "Representation":

                        break;

                    case "Unit":
                        unit = node.InnerText;
                        break;

                    default:
                        break;
                }
            }

            return new GenInteger(categoryPropreties, min, max, inc, null, null, null, IncrementMode.fixedIncrement, representation, value, unit, pValue);
        }
        private async Task<ICategory> GetCommandCategory(XmlNode xmlNode)
        {
            Dictionary<string, IMathematical> expressions;
            IPValue pValue = null;
            var categoryProperties = await GetCategoryProperties(xmlNode);

            Int64 commandValue = 0;
            var commandValueNode = SelectSingleNode(xmlNode, "CommandValue");
            if (commandValueNode != null)
                Int64.TryParse(commandValueNode.InnerText, out commandValue);

            var pValueNode = SelectSingleNode(xmlNode, "pValue");

            var pNode = await ReadPNode(pValueNode.InnerText);

            if (pNode != null)
            {
                pValue = await GetRegister(pNode);
                if (pValue is null)
                    pValue = await GetFormula(pNode);
            }

            return new GenCommand(categoryProperties, commandValue, pValue, null);
        }
        private async Task<IPValue> GetRegister(XmlNode xmlNode)
        {
            XmlNode structEntryNode = null;
            if (xmlNode.ParentNode.Name == nameof(RegisterType.StructReg))
            {
                structEntryNode = xmlNode.Clone();
                xmlNode = xmlNode.ParentNode;
            }
            if (!Enum.GetNames<RegisterType>().Where(x => x.Equals(xmlNode.Name) && x != "StructEntry").Any())
                return null;


            if (xmlNode.Name == nameof(RegisterType.Integer))
                return await GetGenInteger(xmlNode);

            if (xmlNode.Name == nameof(RegisterType.Float))
                return await GetFloatCategory(xmlNode) as IPValue;

            var genRegister = await XmlNodeToGenRegister(xmlNode);

            switch (xmlNode.Name)
            {
                case nameof(RegisterType.IntReg):
                case nameof(RegisterType.FloatReg):
                    return new GenIntReg(genRegister.address, genRegister.length, genRegister.accessMode, null, genRegister.pAddress, GenPort);

                case nameof(RegisterType.StructReg):
                case nameof(RegisterType.MaskedIntReg):
                    if (xmlNode.Name.Equals(nameof(RegisterType.StructReg)))
                    {
                        xmlNode = structEntryNode;
                    }
                    short? lsb = null, msb = null;
                    byte? bit = null;
                    var lsbNode = SelectSingleNode(xmlNode, "LSB");
                    var msbNode = SelectSingleNode(xmlNode, "MSB");
                    var bitNode = SelectSingleNode(xmlNode, "Bit");

                    if (lsbNode != null)
                        lsb = short.Parse(lsbNode.InnerText);
                    if (msbNode != null)
                        msb = short.Parse(msbNode.InnerText);
                    if (bitNode != null)
                        bit = byte.Parse(bitNode.InnerText);

                    var signNode = SelectSingleNode(xmlNode, "Sign");
                    Sign? sign = null;
                    if (signNode != null)
                        sign = Enum.Parse<Sign>(signNode.InnerText);

                    return new GenMaskedIntReg(genRegister.address, genRegister.length, msb, lsb, bit, sign, genRegister.accessMode, genRegister.pAddress, GenPort);
            }

            return null;
        }
        private async Task<(Int64? address, object pAddress, ushort length, GenAccessMode accessMode)> XmlNodeToGenRegister(XmlNode xmlNode)
        {
            Int64? address = null;
            object pAddress = null;
            ushort length = 0;
            GenAccessMode accessMode = GenAccessMode.NA;
            var addressNode = SelectSingleNode(xmlNode, "Address");

            if (addressNode != null)
            {
                if (addressNode.InnerText.StartsWith("0x"))
                    address = Int64.Parse(addressNode.InnerText.Substring(2), System.Globalization.NumberStyles.HexNumber);
                else
                    address = Int64.Parse(addressNode.InnerText);
            }
            var lengthNode = SelectSingleNode(xmlNode, "Length");
            if (lengthNode != null)
            {
                length = ushort.Parse(lengthNode.InnerText);
            }
            if (SelectSingleNode(xmlNode, "AccessMode") is XmlNode accessModeNode)
            {
                accessMode = (GenAccessMode)Enum.Parse(typeof(GenAccessMode), accessModeNode.InnerText);

            }

            if (SelectSingleNode(xmlNode, nodeName: "pAddress") is XmlNode pFeatureNode)
            {
                var pNode = await ReadPNode(pFeatureNode.InnerText);

                if (pNode != null)
                {
                    switch (pNode.Name)
                    {
                        case "IntSwissKnife":
                        case "SwissKnife":
                            pAddress = await GetIntSwissKnife(pNode);
                            break;

                        case "IntConverter":
                        case "Converter":
                            pAddress = await GetConverter(pNode);
                            break;

                        default:
                            pAddress = await GetRegister(pNode);
                            if (pAddress is null)
                                pAddress = await GetGenCategory(pNode);
                            break;
                    }

                    if (pAddress is null)
                        pAddress = await GetGenCategory(pNode);
                }

            }
            return (address, pAddress, length, accessMode);
        }
        private async Task<IPValue> GetGenInteger(XmlNode xmlNode)
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
                        var pValueNode = await ReadPNode(node.InnerText);
                        if (pValueNode != null)
                        {
                            pValue = await GetRegister(pValueNode);
                            if (pValue is null)
                                pValue = await GetIntSwissKnife(pValueNode);
                            if (pValue is null)
                                pValue = await GetConverter(pValueNode);
                        }
                        break;

                    default:
                        break;
                }
            }
            return new GenInteger(null, null, null, null, null, null, null, null, Representation.PureNumber, value, "", pValue);

        }
        public async Task<IMathematical> GetFormula(XmlNode xmlNode)
        {
            if (xmlNode.Name == "IntConverter" || xmlNode.Name == "Converter")
                return await GetConverter(xmlNode);

            if (xmlNode.Name == "IntSwissKnife" || xmlNode.Name != "SwissKnife")
                return await GetIntSwissKnife(xmlNode);

            return null;
        }
        private async Task<IntSwissKnife> GetIntSwissKnife(XmlNode xmlNode)
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
                        var pNode = await ReadPNode(node.InnerText);
                        if (pNode != null)
                        {
                            switch (pNode.Name)
                            {
                                case "IntSwissKnife":
                                case "SwissKnife":
                                    pVariable = await GetIntSwissKnife(pNode);
                                    break;

                                case "IntConverter":
                                case "Converter":
                                    pVariable = await GetConverter(pNode);
                                    break;

                                default:
                                    pVariable = await GetRegister(pNode);
                                    if (pVariable is null)
                                        pVariable = await GetGenCategory(pNode) as IPValue;
                                    break;
                            }

                            if (pVariable is null)
                                pVariable = await GetGenCategory(pNode) as IPValue;

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
        private async Task<Converter> GetConverter(XmlNode xmlNode)
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
                        var pNode = await ReadPNode(node.InnerText);
                        if (pNode != null)
                        {
                            switch (pNode.Name)
                            {
                                case "IntSwissKnife":
                                case "SwissKnife":
                                    pVariable = await GetIntSwissKnife(pNode);
                                    break;

                                case "IntConverter":
                                case "Converter":
                                    pVariable = await GetConverter(pNode);
                                    break;

                                default:
                                    pVariable = await GetRegister(pNode);
                                    if (pVariable is null)
                                        pVariable = await GetGenCategory(pNode) as IPValue;
                                    break;
                            }

                            if (pVariable is null)
                                pVariable = await GetGenCategory(pNode) as IPValue;

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
                        var pValueNode = await ReadPNode(node.InnerText);
                        if (pValueNode != null)
                        {
                            pValue = await GetRegister(pValueNode);
                            if (pValue is null)
                                pValue = await GetIntSwissKnife(pValueNode);
                            if (pValue is null)
                                pValue = await GetConverter(pValueNode);
                            if (pValue is null)
                                pValue = await GetGenCategory(pValueNode) as IPValue;
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
        private async Task<CategoryProperties> GetCategoryProperties(XmlNode xmlNode)
        {
            if (xmlNode.Name == "pFeature")
                xmlNode = await ReadPNode(xmlNode.InnerText);

            GenVisibility visibilty = GenVisibility.Beginner;
            string displayName = "", toolTip = "", description = "";
            bool isStreamable = false;

            //if (xmlNode.SelectSingleNode( "DisplayName") is XmlNode displayNameNode)
            //    displayName = displayNameNode.InnerText;

            if (displayName == "")
            {
                displayName = xmlNode.Attributes["Name"].Value;
            }

            if (SelectSingleNode(xmlNode, "Visibility") is XmlNode visibilityNode)
                visibilty = Enum.Parse<GenVisibility>(visibilityNode.InnerText);
            if (SelectSingleNode(xmlNode, "ToolTip") is XmlNode toolTipNode)
                toolTip = toolTipNode.InnerText;

            if (SelectSingleNode(xmlNode, "Description") is XmlNode descriptionNode)
                description = descriptionNode.InnerText;

            var isStreamableNode = SelectSingleNode(xmlNode, "Streamable");

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
        private XmlNodeList GetAllNodesByAttirbuteValue(string elementName = "*", string attirbuteValue = null, string attirbuteName = "Name")
        {
            var xpath = $"//{namespacePrefix}:{elementName}";
            bool hasElementName = !string.IsNullOrEmpty(elementName);
            bool hasAttirbuteName = !string.IsNullOrEmpty(attirbuteName);
            bool hasAttirbuteValue = !string.IsNullOrEmpty(attirbuteValue);

            string attirbute = string.Empty;

            if (hasAttirbuteName)
            {
                attirbute = $"@{attirbuteName}";
            }
            if (hasAttirbuteValue)
            {
                attirbute += $"='{attirbuteValue}'";
            }

            if (hasElementName && hasAttirbuteName)
            {
                xpath = $"{xpath}[{attirbute}]";
            }
            else
            {
                xpath += attirbute;
            }
            var nodes = xmlDocument.SelectNodes(xpath, xmlNamespaceManager);
            return nodes;
        }

        private XmlNode GetNodeByAttirbuteValue(string elementName = "*", string attirbuteValue = null, string attirbuteName = "Name")
        {
            var xpath = $"//{namespacePrefix}:{elementName}";
            bool hasElementName = !string.IsNullOrEmpty(elementName);
            bool hasAttirbuteName = !string.IsNullOrEmpty(attirbuteName);
            bool hasAttirbuteValue = !string.IsNullOrEmpty(attirbuteValue);

            string attirbute = string.Empty;

            if (hasAttirbuteName)
            {
                attirbute = $"@{attirbuteName}";
            }
            if (hasAttirbuteValue)
            {
                attirbute += $"='{attirbuteValue}'";
            }

            if (hasElementName && hasAttirbuteName)
            {
                xpath = $"{xpath}[{attirbute}]";
            }
            else
            {
                xpath += attirbute;
            }
            var node = xmlDocument.SelectSingleNode(xpath, xmlNamespaceManager);
            return node;
        }
        private async Task<XmlNode> ReadPNode(string pNode)
        {
            if (GetNodeByAttirbuteValue(attirbuteValue: pNode, attirbuteName: "Name") is XmlNode categoryNode)
            {
                return categoryNode;
            }

            throw new NotImplementedException();
        }
        private XmlNode SelectSingleNode(XmlNode xmlNode, string nodeName)
        {
            return xmlNode.SelectSingleNode(namespacePrefix + ':' + nodeName, xmlNamespaceManager);
        }
        private XmlNodeList SelectNodes(XmlNode xmlNode, string nodeName, bool isAllNodes = false)
        {
            string xpath = string.Empty;
            if (isAllNodes)
            {
                xpath = "//" + namespacePrefix + ':' + nodeName;
            }
            else
            {
                xpath = namespacePrefix + ':' + nodeName;
            }
            return xmlNode.SelectNodes(xpath, xmlNamespaceManager);
        }
        #endregion XML Mapping Helpers
        public async Task<(IPValue pValue, IRegister register)> GetRegisterByName(string name)
        {
            (IPValue pValue, IRegister register) tuple = new(null, null);
            if (GetAllNodesByAttirbuteValue(attirbuteValue: name) is XmlNodeList xmlNodeList)
            {
                ICategory category = null;
                foreach (XmlNode node in xmlNodeList)
                {
                    category = await GetGenCategory(node);
                }

                if (category.PValue is IPValue pValue)
                {
                    tuple.pValue = pValue;
                }
                if (category.PValue is IRegister register)
                {
                    tuple.register = register;
                }
            }
            return tuple;
        }
    }

}