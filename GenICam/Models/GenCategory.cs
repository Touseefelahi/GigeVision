using System;
using System.Collections.Generic;

namespace GenICam
{
    public class GenCategory : ICategory
    {
        public CategoryProperties CategoryProperties { get; internal set; }
        public Dictionary<string, ICategory> PFeatures { get; set; }

        public Dictionary<string, ICategory> GetFeatures()
        {
            return PFeatures;
        }
    }
}