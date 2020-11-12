using System;
using System.Collections.Generic;
using System.Text;

namespace GenICam
{
    public interface ICategory : IGenCategory
    {
        Dictionary<string, ICategory> PFeatures { get; set; }
    }
}