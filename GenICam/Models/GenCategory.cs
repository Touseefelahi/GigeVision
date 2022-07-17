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
        public System.Windows.Input.ICommand SetValueCommand { get; internal set; }
        public System.Windows.Input.ICommand GetValueCommand { get; internal set; }
        public GenAccessMode AccessMode { get; set; }

        public List<ICategory> GetFeatures()
        {
            return PFeatures;
        }
    }
}