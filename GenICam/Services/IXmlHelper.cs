using System.Collections.Generic;

namespace GenICam
{
    public interface IXmlHelper
    {
        List<ICategory> CategoryDictionary { get; }
    }
}