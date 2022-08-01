using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GenICam
{
    public class GenCategory : BindableBase, ICategory
    {
        public CategoryProperties CategoryProperties { get; internal set; }
        public List<ICategory> PFeatures { get; set; }

        public IPValue PValue { get; internal set; }

        public string GroupName { get; internal set; }
        public ICommand SetValueCommand { get; internal set; }
        public ICommand GetValueCommand { get; internal set; }

        public Dictionary<string, IMathematical> Expressions { get; internal set; }

        public List<ICategory> GetFeatures()
        {
            return PFeatures;
        }
    }
}