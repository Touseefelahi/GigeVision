using System;
using System.Collections.Generic;
using System.Text;

namespace GenICam
{
    /// <summary>
    /// Maps to an entry in a tree structuring the camera's features
    /// </summary>
    public interface ICategory : INode
    {
        CategoryProperties CategoryProperties { get; }
        List<ICategory> PFeatures { get; set; }
        public IPValue PValue { get; }
    }
}