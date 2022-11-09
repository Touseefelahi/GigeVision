using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

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
        private const string NodePInc = "pInc";
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
        /// <exception cref="GenICamException">GenICamException.</exception>
        public XmlHelper(XmlDocument xmlDocument, IPort genPort)
        {
            try
            {
                xmlRoot = xmlDocument["RegisterDescription"];
                var uri = xmlRoot.Attributes["xmlns"].Value;
                namespacePrefix = xmlRoot.Attributes["StandardNameSpace"].Value;
                xmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
                xmlNamespaceManager.AddNamespace(namespacePrefix, uri);
                this.xmlDocument = xmlDocument;
                GenPort = genPort;
            }
            catch (NullReferenceException ex)
            {
                throw new GenICamException("Invalid XML document been provided", ex);
            }
            catch (ArgumentNullException ex)
            {
                throw new GenICamException("Invalid XML document been provided", ex);
            }
            catch (ArgumentException ex)
            {
                throw new GenICamException("Invalid XML document been provided", ex);
            }
            catch (Exception ex)
            {
                throw new GenICamException("Invalid XML document been provided", ex);
            }
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
        public async Task LoadUp(bool isReadAllRegisters = false)
        {
            try
            {
                CategoryDictionary = new List<ICategory>();
                rootCategoryNode = xmlRoot.SelectSingleNode("//" + namespacePrefix + ":Category[@Name=\"Root\"]", xmlNamespaceManager);
            }
            catch (XPathException ex)
            {
                throw new GenICamException("Failed to find the root", ex);
            }

            try
            {
                if (isReadAllRegisters)
                {
                    if ((await GetCategoryFeatures(rootCategoryNode)) is ICategory category)
                    {
                        CategoryDictionary.Add(category);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new GenICamException("Failed to read category feature", ex);
            }
        }

        [Obsolete]
        /// <summary>
        /// Get the formula associated to a node.
        /// </summary>
        /// <param name="xmlNode">The node where the formula is.</param>
        /// <returns>A mathematical formula or null if not found.</returns>
        private async Task<IMathematical> GetFormula(XmlNode xmlNode)
        {
            try
            {
                if (xmlNode.Name == "IntConverter" || xmlNode.Name == "Converter")
                {
                    return await GetConverter(xmlNode);
                }

                if (xmlNode.Name == "IntSwissKnife" || xmlNode.Name == "SwissKnife")
                {
                    return await GetIntSwissKnife(xmlNode);
                }

                throw new GenICamException("Failed to get the feature formula", new NotImplementedException());
            }
            catch (Exception ex)
            {
                throw new GenICamException("Failed to get the feature formula", ex);
            }
        }

        /// <summary>
        /// Gets register by name.
        /// </summary>
        /// <param name="name">The name of the register.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<(IPValue pValue, IRegister register)> GetRegisterByName(string name)
        {
            try
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
            catch (Exception ex)
            {
                throw new GenICamException($"Failed to get the feature by the given name: {name}", ex);
            }
        }

        private async Task<ICategory> GetCategoryFeatures(XmlNode categoryNode)
        {
            try
            {
                string groupName = categoryNode.Attributes[NodeName].Value;
                var genCategory = new GenCategory(GetCategoryProperties(categoryNode), null) { GroupName = groupName };

                genCategory.PFeatures = await ReadCategoryFeature(categoryNode);
                return genCategory;
            }
            catch (Exception ex)
            {
                throw new GenICamException($"Failed to get the category feature by the given node: {categoryNode.Attributes["Name"].Value}", ex);
            }
        }

        #region GenIcam Getters
        private async Task<List<ICategory>> ReadCategoryFeature(XmlNode node)
        {
            try
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
            catch (Exception ex)
            {
                throw new GenICamException($"Failed to read Category feature by the given node {node.Name}", ex);
            }
        }

        private async Task<ICategory> GetGenCategory(XmlNode node)
        {
            try
            {
                ICategory genCategory = null;
                switch (node.Name)
                {
                    case nameof(CategoryType.StringReg):
                        genCategory = GetStringCategory(node);
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
            catch (Exception ex)
            {
                throw new GenICamException($"Failed to get Gen Category by the given node {node.Name}", ex);
            }
        }

        private async Task<ICategory> GetFloatCategory(XmlNode xmlNode)
        {
            try
            {
                var categoryPropreties = GetCategoryProperties(xmlNode);

                Dictionary<string, IPValue> expressions = new Dictionary<string, IPValue>();
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
                            pNode = ReadPNode(node.InnerText);
                            if (pNode != null)
                            {
                                expressions.Add(node.Name, await GetPValue(pNode));
                            }

                            break;

                        case NodeInc:
                            _ = long.TryParse(node.InnerText, out inc);
                            break;

                        case NodePValue:
                            pNode = ReadPNode(node.InnerText);
                            if (pNode != null)
                            {
                                    pValue = await GetPValue(pNode);
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

                return new GenFloat(categoryPropreties, min, max, inc, IncrementMode.fixedIncrement, representation, value, unit, pValue);
            }
            catch (Exception ex)
            {
                throw new GenICamException($"Failed to get Float Category by the given node {xmlNode.Name}", ex);
            }
        }

        private async Task<ICategory> GetBooleanCategory(XmlNode xmlNode)
        {
            try
            {
                var categoryPropreties = GetCategoryProperties(xmlNode);

                Dictionary<string, IMathematical> expressions = new Dictionary<string, IMathematical>();

                IPValue pValue = null;
                if (SelectSingleNode(xmlNode, NodePValue) is XmlNode pValueNode)
                {
                    XmlNode pNode = ReadPNode(pValueNode.InnerText);
                    if (pNode != null)
                    {
                            pValue = await GetPValue(pNode);
                    }
                }

                return new GenBoolean(categoryPropreties, pValue);
            }
            catch (Exception ex)
            {
                throw new GenICamException($"Failed to get Boolean Category by the given node {xmlNode.Name}", ex);
            }
        }

        private async Task<ICategory> GetEnumerationCategory(XmlNode xmlNode)
        {
            try
            {
                var categoryProperties = GetCategoryProperties(xmlNode);

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
                    ////    isImplementedExpr = ReadPNode(isImplementedNode.InnerText);
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
                    var pNode = ReadPNode(enumPValue.InnerText);
                    if (pNode != null)
                    {
                            pValue = await GetPValue(pNode);
                    }
                }

                return new GenEnumeration(categoryProperties, entry, pValue);
            }
            catch (Exception ex)
            {
                throw new GenICamException($"Failed to get Enumeration Category by the given node {xmlNode.Name}", ex);
            }
        }

        private ICategory GetStringCategory(XmlNode xmlNode)
        {
            try
            {
                var categoryProperties = GetCategoryProperties(xmlNode);

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

                return new GenStringReg(categoryProperties, address, length, accessMode, GenPort, null);
            }
            catch (Exception ex)
            {
                throw new GenICamException($"Failed to get String Category by the given node {xmlNode.Name}", ex);
            }
        }

        private async Task<ICategory> GetIntegerCategory(XmlNode xmlNode)
        {
            try
            {
                var categoryPropreties = GetCategoryProperties(xmlNode);

                long? min = 0, max = 0, inc = 0, value = 0;
                string unit = string.Empty;
                Representation representation = Representation.PureNumber;
                XmlNode pNode;

                Dictionary<string, IMathematical> expressions = new Dictionary<string, IMathematical>();

                IPValue pValue = null;
                IPValue pMax = null;
                IPValue pMin = null;
                IPValue pInc = null;
                IPValue pSelected = null;

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

                        case NodeInc:
                            inc = Convert.ToInt64(node.InnerText, convertFromBase);
                            break;

                        case NodePMin:
                            pNode = ReadPNode(node.InnerText);
                            pMin = await PNodeToPValue(pNode);

                            break;
                        case NodePMax:
                            pNode = ReadPNode(node.InnerText);
                            pMax = await PNodeToPValue(pNode);
                            break;

                        case NodePInc:
                            pNode = ReadPNode(node.InnerText);
                            pInc = await PNodeToPValue(pNode);
                            break;

                        case NodePValue:
                            pNode = ReadPNode(node.InnerText);
                            pValue = await PNodeToPValue(pNode);
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

                return new GenInteger(categoryPropreties, min, max, inc, pMax, pMin, pInc, IncrementMode.fixedIncrement, representation, value, unit, pValue);
            }
            catch (Exception ex)
            {
                throw new GenICamException($"Failed to get Integer Category by the given node {xmlNode.Name}", ex);
            }
        }

        private async Task<IPValue> PNodeToPValue(XmlNode pNode)
        {
            IPValue pValue = null;
            if (pNode != null)
            {
                pValue = await GetPValue(pNode);
            }

            return pValue;
        }

        private async Task<ICategory> GetCommandCategory(XmlNode xmlNode)
        {
            try
            {
                IPValue pValue = null;
                var categoryProperties = GetCategoryProperties(xmlNode);

                long commandValue = 0;
                var commandValueNode = SelectSingleNode(xmlNode, "CommandValue");
                if (commandValueNode != null)
                {
                    long.TryParse(commandValueNode.InnerText, out commandValue);
                }

                var pValueNode = SelectSingleNode(xmlNode, NodePValue);

                var pNode = ReadPNode(pValueNode.InnerText);

                if (pNode != null)
                {
                    pValue = await GetPValue(pNode);
                }

                return new GenCommand(categoryProperties, commandValue, pValue);
            }
            catch (Exception ex)
            {
                throw new GenICamException($"Failed to get Command Category by the given node {xmlNode.Name}", ex);
            }
        }

        private async Task<IPValue> GetPValue(XmlNode xmlNode)
        {
            try
            {
                XmlNode structEntryNode = null;
                if (xmlNode.ParentNode.Name == nameof(RegisterType.StructReg))
                {
                    structEntryNode = xmlNode.Clone();
                    xmlNode = xmlNode.ParentNode;
                }

                if (Enum.GetNames<RegisterType>().Where(x => x.Equals(xmlNode.Name) && x != "StructEntry").Any())
                {
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

                    throw new GenICamException(message: $"Failed to find GenRegister type by the given node name {xmlNode.Name}", new NotImplementedException());
                }
                else if (xmlNode.Name == "IntConverter" || xmlNode.Name == "Converter")
                {
                    return await GetConverter(xmlNode);
                }
                else if (xmlNode.Name == "IntSwissKnife" || xmlNode.Name == "SwissKnife")
                {
                    return await GetIntSwissKnife(xmlNode);
                }
                else
                {
                    throw new GenICamException($"Failed to get pValue by the given node {xmlNode.Name}", new NotImplementedException());
                }
            }
            catch (Exception ex)
            {
                throw new GenICamException($"Failed to get pValue by the given node {xmlNode.Name}", ex);
            }
        }

        private async Task<(long? address, object pAddress, ushort length, GenAccessMode accessMode)> XmlNodeToGenRegister(XmlNode xmlNode)
        {
            try
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
                    var pNode = ReadPNode(pFeatureNode.InnerText);
                    if (pNode != null)
                    {
                                pAddress = await GetPValue(pNode);
                                pAddress ??= await GetGenCategory(pNode);
                    }
                }

                return (address, pAddress, length, accessMode);
            }
            catch (Exception ex)
            {
                throw new GenICamException($"Failed to map XML node to Gen register by the given node {xmlNode.Name}", ex);
            }
        }

        private async Task<IPValue> GetGenInteger(XmlNode xmlNode)
        {
            try
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
                            var pNode = ReadPNode(node.InnerText);
                            if (pNode != null)
                            {
                                pValue = await GetPValue(pNode);
                            }

                            break;

                        default:
                            break;
                    }
                }

                return new GenInteger(null, null, null, null, null, null, null, null, Representation.PureNumber, value, string.Empty, pValue);
            }
            catch (Exception ex)
            {
                throw new GenICamException($"Failed to get Gen Integer by the given node {xmlNode.Name}", ex);
            }
        }

        private async Task<IntSwissKnife> GetIntSwissKnife(XmlNode xmlNode)
        {
            try
            {
                if (xmlNode.Name != "IntSwissKnife" && xmlNode.Name != "SwissKnife")
                {
                    throw new GenICamException(message: $"Failed to verify SwissKnife node by the given name {xmlNode.Name}", new ArgumentException());
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
                            var pNode = ReadPNode(node.InnerText);
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
                                        pVariable = await GetPValue(pNode);
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
            catch (Exception ex)
            {
                throw new GenICamException($"Failed to get IntSwissKnife by the given node {xmlNode.Name}", ex);
            }
        }

        private async Task<Converter> GetConverter(XmlNode xmlNode)
        {
            try
            {
                if (xmlNode.Name != "IntConverter" && xmlNode.Name != "Converter")
                {
                    throw new GenICamException(message: $"Failed to verify Converter node by the given name {xmlNode.Name}", new ArgumentException());
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
                            var pNode = ReadPNode(node.InnerText);
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
                                        pVariable = await GetPValue(pNode);
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
                            var pValueNode = ReadPNode(node.InnerText);
                            if (pValueNode != null)
                            {
                                pValue = await GetPValue(pValueNode);
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
            catch (Exception ex)
            {
                throw new GenICamException($"Failed to get the Converter by the given node {xmlNode.Name}", ex);
            }
        }

        /// <summary>
        /// Get Category Properties such as Name, AccessMode and Visibility.
        /// </summary>
        /// <param name="xmlNode">The XML node to get the category property.</param>
        /// <returns>A Category property.</returns>
        private CategoryProperties GetCategoryProperties(XmlNode xmlNode)
        {
            try
            {
                if (xmlNode.Name == NodePFeature)
                {
                    xmlNode = ReadPNode(xmlNode.InnerText);
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
            catch (Exception ex)
            {
                throw new GenICamException($"Failed to get category properties by the given node {xmlNode.Name}", ex);
            }
        }

        #endregion GenIcam Getters

        #region XML Mapping Helpers
        private XmlNodeList GetAllNodesByAttirbuteValue(string elementName = "*", string attirbuteValue = null, string attirbuteName = NodeName)
        {
            try
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
            catch (Exception ex)
            {
                throw new GenICamException(message: $"Failed to get all nodes by the given node name {elementName}, attribute name {attirbuteName} and attribute value {attirbuteValue}", ex);
            }
        }

        private XmlNode GetNodeByAttirbuteValue(string elementName = "*", string attirbuteValue = null, string attirbuteName = NodeName)
        {
            try
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
            catch (Exception ex)
            {
                throw new GenICamException(message: $"Failed to get the node by the given node name {elementName}, attribute name {attirbuteName} and attribute value {attirbuteValue}", ex);
            }
        }

        private XmlNode ReadPNode(string pNode)
        {
            try
            {
                if (GetNodeByAttirbuteValue(attirbuteValue: pNode, attirbuteName: NodeName) is XmlNode categoryNode)
                {
                    return categoryNode;
                }

                throw new GenICamException(message: $"Failed to read XML node by given feature name {pNode}", new NotImplementedException());
            }
            catch (Exception ex)
            {
                throw new GenICamException(message: $"Failed to read XML node by given feature name {pNode}", ex);
            }
        }

        private XmlNode SelectSingleNode(XmlNode xmlNode, string nodeName)
        {
            try
            {
                return xmlNode.SelectSingleNode(namespacePrefix + ':' + nodeName, xmlNamespaceManager);
            }
            catch (Exception ex)
            {
                throw new GenICamException($"Failed to find a single node by the given name {nodeName}", ex);
            }
        }

        private XmlNodeList SelectNodes(XmlNode xmlNode, string nodeName, bool isAllNodes = false)
        {
            try
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
            catch (Exception ex)
            {
                throw new GenICamException($"Failed to find nodes by the given name {nodeName}", ex);
            }
        }

        #endregion XML Mapping Helpers
    }
}