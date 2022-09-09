using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace GenICam
{
    /// <summary>
    /// this class helps Gvcp to read all the registers from XML file.
    /// </summary>
    public class XmlHelper : IXmlHelper
    {
        private const string NodePValue = "pValue";
        private const string NodeName = "Name";
        private const string NodePFeature = "pFeature";
        private const string NodeValue = "Value";
        private const string NodeMin = "Min";
        private const string NodePMin = "pMin";
        private const string NodeMax = "Max";
        private const string NodePMax = "pMax";
        private const string NodeInc = "Inc";
        private const string NodeAddress = "Address";
        private const string NodePAddress = "pAddress";
        private const string NodeAccessMode = "AccessMode";
        private const string NodeLength = "Length";
        private const string NodeRepresentation = "Representation";

        #region XML Setup

        private XmlNode xmlRoot;
        private string namespacePrefix;
        private XmlNamespaceManager xmlNamespaceManager;
        private XmlDocument xmlDocument;
        private XmlNode rootCategoryNode;

        #endregion XML Setup

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlHelper"/> class.
        /// </summary>
        /// <param name="xmlDocument">XML File.</param>
        /// <param name="genPort">The GenICam port.</param>
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
        /// Gets the GenICam port.
        /// </summary>
        public IPort GenPort { get; }

        /// <inheritdoc/>
        public List<ICategory> CategoryDictionary { get; private set; }

        /// <summary>
        /// The nodes can either be retrieved by their(unique) name or can be found by traversing the node graph starting with the root node.
        /// </summary>
        /// <param name="isReadAllRegisters">True to load in the Category Dictionary all the categories.</param>
        /// <returns>True if success.</returns>
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

        /// <summary>
        /// Get the formula associated to a node.
        /// </summary>
        /// <param name="xmlNode">The node where the formula is.</param>
        /// <returns>A mathematical formula or null if not found.</returns>
        public async Task<IMathematical> GetFormula(XmlNode xmlNode)
        {
            if (xmlNode.Name == "IntConverter" || xmlNode.Name == "Converter")
            {
                return await GetConverter(xmlNode);
            }

            if (xmlNode.Name == "IntSwissKnife" || xmlNode.Name != "SwissKnife")
            {
                return await GetIntSwissKnife(xmlNode);
            }

            return null;
        }

        /// <summary>
        /// Gets register by name.
        /// </summary>
        /// <param name="name">The name of the register.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<(IPValue pValue, IRegister register)> GetRegisterByName(string name)
        {
            (IPValue pValue, IRegister register) tuple = new(null, null);
            if (GetAllNodesByAttirbuteValue(attirbuteValue: name) is XmlNodeList xmlNodeList)
            {
                ICategory category = null;
                foreach (XmlNode node in xmlNodeList)
                {
                    category = await GetGenCategory(node);
                    if (category is null)
                    {
                        continue;
                    }

                    if (category.PValue is IPValue pValue)
                    {
                        tuple.pValue = pValue;
                    }

                    if (category.PValue is IRegister register)
                    {
                        tuple.register = register;
                    }

                    return tuple;
                }
            }

            return tuple;
        }

        private async Task<ICategory> GetCategoryFeatures(XmlNode categoryNode)
        {
            string groupName = categoryNode.Attributes[NodeName].Value;
            var genCategory = new GenCategory() { GroupName = groupName, CategoryProperties = await GetCategoryProperties(categoryNode) };

            genCategory.PFeatures = await ReadCategoryFeature(categoryNode);
            return genCategory;
        }

        #region GenIcam Getters
        private async Task<List<ICategory>> ReadCategoryFeature(XmlNode node)
        {
            var pFeatures = new List<ICategory>();
            ICategory category;
            var pFeatureNodes = SelectNodes(node, NodePFeature);
            foreach (XmlNode pFeatureNode in pFeatureNodes)
            {
                XmlNodeList pNodes = GetAllNodesByAttirbuteValue(attirbuteValue: pFeatureNode.InnerText, attirbuteName: NodeName);
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
            long inc = 0;
            string unit = string.Empty;
            Representation representation = Representation.PureNumber;
            XmlNode pNode;
            foreach (XmlNode node in xmlNode.ChildNodes)
            {
                switch (node.Name)
                {
                    case NodeValue:
                        _ = double.TryParse(node.InnerText, out value);
                        break;

                    case NodeMin:
                        _ = double.TryParse(node.InnerText, out min);
                        break;

                    case NodeMax:
                        _ = double.TryParse(node.InnerText, out max);
                        break;

                    case NodePMin:
                    case NodePMax:
                        pNode = await ReadPNode(node.InnerText);
                        if (pNode != null)
                        {
                            expressions.Add(node.Name, await GetFormula(pNode));
                        }

                        break;

                    case NodeInc:
                        _ = long.TryParse(node.InnerText, out inc);
                        break;

                    case NodePValue:
                        pNode = await ReadPNode(node.InnerText);
                        if (pNode != null)
                        {
                            pValue = await GetRegister(pNode);
                            pValue ??= await GetFormula(pNode);
                        }

                        break;

                    case NodeRepresentation:
                        _ = Enum.TryParse<Representation>(node.InnerText, out representation);
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
            if (SelectSingleNode(xmlNode, NodePValue) is XmlNode pValueNode)
            {
                XmlNode pNode = await ReadPNode(pValueNode.InnerText);
                if (pNode != null)
                {
                    pValue = await GetRegister(pNode);
                    if (pValue is null)
                    {
                        pValue = await GetFormula(pNode);
                    }
                }
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

                // Keeping this dead code on pupose.
                ////XmlNode isImplementedNode = SelectSingleNode(enumEntry, "pIsImplemented");
                ////XmlNode isImplementedExpr = null;
                ////if (isImplementedNode != null)
                ////{
                ////    isImplementedExpr = await ReadPNode(isImplementedNode.InnerText);
                ////    if (isImplementedExpr != null)
                ////    {
                ////        isImplementedValue = await GetRegister(isImplementedExpr);
                ////        if (isImplementedValue is null)
                ////            isImplementedValue = await GetFormula(isImplementedExpr);
                ////        if (isImplementedValue is null)
                ////            isImplementedValue = await GetGenCategory(isImplementedExpr);
                ////    }
                ////}
                uint entryValue;
                uint.TryParse(SelectSingleNode(enumEntry, NodeValue).InnerText, out entryValue);
                entry.Add(enumEntry.Attributes[NodeName].Value, new EnumEntry(entryValue, isImplementedValue));
            }

            IPValue pValue = null;

            var enumPValue = SelectSingleNode(xmlNode, NodePValue);
            if (enumPValue != null)
            {
                var enumPValueNode = await ReadPNode(enumPValue.InnerText);
                if (enumPValueNode != null)
                {
                    pValue = await GetRegister(enumPValueNode);
                    if (pValue is null)
                    {
                        pValue = await GetFormula(enumPValueNode);
                    }
                }
            }

            return new GenEnumeration(categoryProperties, entry, pValue);
        }

        private async Task<ICategory> GetStringCategory(XmlNode xmlNode)
        {
            var categoryProperties = await GetCategoryProperties(xmlNode);

            long address = 0;
            var addressNode = SelectSingleNode(xmlNode, NodeAddress);
            if (addressNode != null)
            {
                if (addressNode.InnerText.StartsWith("0x"))
                {
                    address = long.Parse(addressNode.InnerText.Substring(2), System.Globalization.NumberStyles.HexNumber);
                }
                else
                {
                    address = long.Parse(addressNode.InnerText);
                }
            }

            ushort length = ushort.Parse(SelectSingleNode(xmlNode, NodeLength).InnerText);
            GenAccessMode accessMode = (GenAccessMode)Enum.Parse(typeof(GenAccessMode), SelectSingleNode(xmlNode, NodeAccessMode).InnerText);

            return new GenStringReg(categoryProperties, address, length, accessMode, GenPort);
        }

        private async Task<ICategory> GetIntegerCategory(XmlNode xmlNode)
        {
            var categoryPropreties = await GetCategoryProperties(xmlNode);

            long? min = 0, max = 0, inc = 0, value = 0;
            string unit = string.Empty;
            Representation representation = Representation.PureNumber;
            XmlNode pNode;

            Dictionary<string, IMathematical> expressions = new Dictionary<string, IMathematical>();

            IPValue pValue = null;

            var representationNode = SelectSingleNode(xmlNode, NodeRepresentation);
            if (representationNode != null)
            {
                _ = Enum.TryParse<Representation>(representationNode.InnerText, out representation);
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
                    case NodeValue:
                        value = Convert.ToInt64(node.InnerText, convertFromBase);

                        break;

                    case NodeMin:
                        min = Convert.ToInt64(node.InnerText, convertFromBase);
                        break;

                    case NodeMax:
                        max = Convert.ToInt64(node.InnerText, convertFromBase);
                        break;

                    case NodePMin:
                        pNode = await ReadPNode(node.InnerText);
                        if (pNode != null)
                        {
                            expressions.Add(node.Name, await GetFormula(pNode));
                        }

                        break;

                    case NodePMax:
                        pNode = await ReadPNode(node.InnerText);
                        if (pNode != null)
                        {
                            expressions.Add(node.Name, await GetFormula(pNode));
                        }

                        break;

                    case NodeInc:
                        inc = Convert.ToInt64(node.InnerText, convertFromBase);

                        break;

                    case NodePValue:

                        pNode = await ReadPNode(node.InnerText);
                        if (pNode != null)
                        {
                            pValue = await GetRegister(pNode);
                            if (pValue is null)
                            {
                                pValue = await GetFormula(pNode);
                            }
                        }

                        break;

                    case NodeRepresentation:
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
            IPValue pValue = null;
            var categoryProperties = await GetCategoryProperties(xmlNode);

            long commandValue = 0;
            var commandValueNode = SelectSingleNode(xmlNode, "CommandValue");
            if (commandValueNode != null)
            {
                long.TryParse(commandValueNode.InnerText, out commandValue);
            }

            var pValueNode = SelectSingleNode(xmlNode, NodePValue);

            var pNode = await ReadPNode(pValueNode.InnerText);

            if (pNode != null)
            {
                pValue = await GetRegister(pNode);
                if (pValue is null)
                {
                    pValue = await GetFormula(pNode);
                }
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
            {
                return null;
            }

            if (xmlNode.Name == nameof(RegisterType.Integer))
            {
                return await GetGenInteger(xmlNode);
            }
            else if (xmlNode.Name == nameof(RegisterType.Float))
            {
                return await GetFloatCategory(xmlNode) as IPValue;
            }

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
                    {
                        lsb = short.Parse(lsbNode.InnerText);
                    }

                    if (msbNode != null)
                    {
                        msb = short.Parse(msbNode.InnerText);
                    }

                    if (bitNode != null)
                    {
                        bit = byte.Parse(bitNode.InnerText);
                    }

                    var signNode = SelectSingleNode(xmlNode, "Sign");
                    Sign? sign = null;
                    if (signNode != null)
                    {
                        sign = Enum.Parse<Sign>(signNode.InnerText);
                    }

                    return new GenMaskedIntReg(genRegister.address, genRegister.length, msb, lsb, bit, sign, genRegister.accessMode, genRegister.pAddress, GenPort);
            }

            return null;
        }

        private async Task<(long? address, object pAddress, ushort length, GenAccessMode accessMode)> XmlNodeToGenRegister(XmlNode xmlNode)
        {
            long? address = null;
            object pAddress = null;
            ushort length = 0;
            GenAccessMode accessMode = GenAccessMode.NA;
            var addressNode = SelectSingleNode(xmlNode, NodeAddress);

            if (addressNode != null)
            {
                if (addressNode.InnerText.StartsWith("0x"))
                {
                    address = long.Parse(addressNode.InnerText.Substring(2), System.Globalization.NumberStyles.HexNumber);
                }
                else
                {
                    address = long.Parse(addressNode.InnerText);
                }
            }

            var lengthNode = SelectSingleNode(xmlNode, NodeLength);
            if (lengthNode != null)
            {
                length = ushort.Parse(lengthNode.InnerText);
            }

            if (SelectSingleNode(xmlNode, NodeAccessMode) is XmlNode accessModeNode)
            {
                accessMode = (GenAccessMode)Enum.Parse(typeof(GenAccessMode), accessModeNode.InnerText);
            }

            if (SelectSingleNode(xmlNode, nodeName: NodePAddress) is XmlNode pFeatureNode)
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
                            pAddress ??= await GetGenCategory(pNode);

                            break;
                    }

                    pAddress ??= await GetGenCategory(pNode);
                }
            }

            return (address, pAddress, length, accessMode);
        }

        private async Task<IPValue> GetGenInteger(XmlNode xmlNode)
        {
            long value = 0;
            IPValue pValue = null;
            foreach (XmlNode node in xmlNode.ChildNodes)
            {
                switch (node.Name)
                {
                    case NodeValue:
                        if (node.InnerText.StartsWith("0x"))
                        {
                            value = long.Parse(node.InnerText.Substring(2), System.Globalization.NumberStyles.HexNumber);
                        }
                        else
                        {
                            _ = long.TryParse(node.InnerText, out value);
                        }

                        break;

                    case NodePValue:
                        var pValueNode = await ReadPNode(node.InnerText);
                        if (pValueNode != null)
                        {
                            pValue = await GetRegister(pValueNode);
                            pValue ??= await GetIntSwissKnife(pValueNode);
                            pValue ??= await GetConverter(pValueNode);
                        }

                        break;

                    default:
                        break;
                }
            }

            return new GenInteger(null, null, null, null, null, null, null, null, Representation.PureNumber, value, string.Empty, pValue);
        }

        private async Task<IntSwissKnife> GetIntSwissKnife(XmlNode xmlNode)
        {
            if (xmlNode.Name != "IntSwissKnife" && xmlNode.Name != "SwissKnife")
            {
                return null;
            }

            Dictionary<string, IPValue> pVariables = new Dictionary<string, IPValue>();
            Dictionary<string, double> constants = new Dictionary<string, double>();
            Dictionary<string, string> expressions = new Dictionary<string, string>();

            string formula = string.Empty;

            foreach (XmlNode node in xmlNode.ChildNodes)
            {
                // child could be either pVariable or Formula
                switch (node.Name)
                {
                    case "pVariable":
                        // pVariable could be IntSwissKnife, SwissKnife, Integer, IntReg, Float, FloatReg,
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
                                    pVariable ??= await GetGenCategory(pNode) as IPValue;
                                    break;
                            }

                            pVariable ??= await GetGenCategory(pNode) as IPValue;

                            pVariables.Add(node.Attributes[NodeName].Value, pVariable);
                        }

                        break;

                    case "Constant":
                        constants.Add(node.Attributes[NodeName].Value, double.Parse(node.InnerText));
                        break;

                    case "Expression":
                        expressions.Add(node.Attributes[NodeName].Value, node.InnerText);
                        break;

                    case "Formula":
                        formula = node.InnerText;
                        break;

                    default:
                        break;
                }
            }

            return new IntSwissKnife(formula, pVariables, constants, expressions);
        }

        private async Task<Converter> GetConverter(XmlNode xmlNode)
        {
            if (xmlNode.Name != "IntConverter" && xmlNode.Name != "Converter")
            {
                return null;
            }

            Dictionary<string, IPValue> pVariables = new Dictionary<string, IPValue>();

            string formulaFrom = string.Empty;
            string formulaTo = string.Empty;
            IPValue pValue = null;
            Slope slope = Slope.None;

            foreach (XmlNode node in xmlNode.ChildNodes)
            {
                // child could be either pVariable or Formula
                switch (node.Name)
                {
                    case "pVariable":
                        // pVariable could be IntSwissKnife, SwissKnife, Integer, IntReg, Float, FloatReg,
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
                                    pVariable ??= await GetGenCategory(pNode) as IPValue;
                                    break;
                            }

                            pVariable ??= await GetGenCategory(pNode) as IPValue;

                            pVariables.Add(node.Attributes[NodeName].Value, pVariable);
                        }

                        break;

                    case "FormulaTo":
                        formulaTo = node.InnerText;
                        break;

                    case "FormulaFrom":
                        formulaFrom = node.InnerText;
                        break;

                    case NodePValue:
                        var pValueNode = await ReadPNode(node.InnerText);
                        if (pValueNode != null)
                        {
                            pValue = await GetRegister(pValueNode);
                            pValue ??= await GetIntSwissKnife(pValueNode);
                            pValue ??= await GetConverter(pValueNode);
                            pValue ??= await GetGenCategory(pValueNode) as IPValue;
                        }

                        break;

                    case "Slope":
                        slope = Enum.Parse<Slope>(node.InnerText);
                        break;

                    default:
                        break;
                }
            }

            return new Converter(formulaTo, formulaFrom, pValue, slope, pVariables);
        }

        /// <summary>
        /// Get Category Properties such as Name, AccessMode and Visibility.
        /// </summary>
        /// <param name="xmlNode">The XML node to get the category property.</param>
        /// <returns>A Category property.</returns>
        private async Task<CategoryProperties> GetCategoryProperties(XmlNode xmlNode)
        {
            if (xmlNode.Name == NodePFeature)
            {
                xmlNode = await ReadPNode(xmlNode.InnerText);
            }

            GenVisibility visibilty = GenVisibility.Beginner;
            string displayName = string.Empty, toolTip = string.Empty, description = string.Empty;
            bool isStreamable = false;

            //// if (xmlNode.SelectSingleNode( "DisplayName") is XmlNode displayNameNode)
            ////    displayName = displayNameNode.InnerText;

            if (displayName == string.Empty)
            {
                displayName = xmlNode.Attributes[NodeName].Value;
            }

            if (SelectSingleNode(xmlNode, "Visibility") is XmlNode visibilityNode)
            {
                visibilty = Enum.Parse<GenVisibility>(visibilityNode.InnerText);
            }

            if (SelectSingleNode(xmlNode, "ToolTip") is XmlNode toolTipNode)
            {
                toolTip = toolTipNode.InnerText;
            }

            if (SelectSingleNode(xmlNode, "Description") is XmlNode descriptionNode)
            {
                description = descriptionNode.InnerText;
            }

            var isStreamableNode = SelectSingleNode(xmlNode, "Streamable");

            if ((isStreamableNode != null) && (isStreamableNode.InnerText == "Yes"))
            {
                isStreamable = true;
            }

            string rootName = string.Empty;

            if (xmlNode.ParentNode.Attributes["Comment"] != null)
            {
                rootName = xmlNode.ParentNode.Attributes["Comment"].Value;
            }

            return new CategoryProperties(rootName, displayName, toolTip, description, visibilty, isStreamable);
        }

        #endregion GenIcam Getters

        #region XML Mapping Helpers
        private XmlNodeList GetAllNodesByAttirbuteValue(string elementName = "*", string attirbuteValue = null, string attirbuteName = NodeName)
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

        private XmlNode GetNodeByAttirbuteValue(string elementName = "*", string attirbuteValue = null, string attirbuteName = NodeName)
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
            if (GetNodeByAttirbuteValue(attirbuteValue: pNode, attirbuteName: NodeName) is XmlNode categoryNode)
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
    }
}