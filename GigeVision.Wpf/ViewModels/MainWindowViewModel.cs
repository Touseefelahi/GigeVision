using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GigeVision.Wpf.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "GigE Device Control";

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public MainWindowViewModel()
        {
        }
    }
}