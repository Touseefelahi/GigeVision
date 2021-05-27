using System;
using System.Collections.Generic;
using System.Text;

namespace GenICam
{
    public interface ICategory : IGenCategory, IIsImplemented
    {
        string GroupName { get; }
        List<ICategory> PFeatures { get; set; }
        IPValue PValue { get; }
        Dictionary<string, IMathematical> Expressions { get; }

        CategoryProperties CategoryProperties { get; }
    }
}