using System.Collections.Generic;

namespace GenICam
{
    public interface IGenCategory
    {
       List<ICategory> GetFeatures();
    }
}