using System.Collections.Generic;

namespace GenICam
{
    public interface IGenCategory
    {
        Dictionary<string, ICategory> GetFeatures();
    }
}