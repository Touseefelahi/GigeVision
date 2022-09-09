using System.Collections.Generic;

namespace GenICam
{
    /// <summary>
    /// Interface for XML Helper.
    /// </summary>
    public interface IXmlHelper
    {
        /// <summary>
        /// Gets the list of categories.
        /// </summary>
        List<ICategory> CategoryDictionary { get; }
    }
}