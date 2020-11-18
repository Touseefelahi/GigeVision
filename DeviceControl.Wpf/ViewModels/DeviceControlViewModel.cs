using DeviceControl.Wpf.DTO;
using GenICam;
using GigeVision.Core;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;

namespace DeviceControl.Wpf.ViewModels
{
    public class DeviceControlViewModel : BindableBase
    {
        private ICategory selectedCategory;

        #region Properties

        /// <summary>
        /// This List is binded to the TreeView as a parent
        /// </summary>
        public List<ICategory> Categories { get; set; }

        public ICommand LoadedWindowCommand { get; }

        public GenVisibility CameraRegisterVisibility
        {
            get;
            set;
        }

        public bool IsBusy { get; set; }

        public bool IsExpanded { get; set; } = false;

        public CameraRegisterDTO SelectedRegister
        {
            get;
            set;
        }

        public Int64 ValueToWrite
        {
            get;
            set;
        }

        #endregion Properties

        #region Commands

        public DelegateCommand TestDataTriggerCommand { get; }
        public DelegateCommand ExpandCommand { get; }
        public DelegateCommand<CameraRegisterDTO> SelectRegisterCommand { get; }
        public IGenPort GenPort { get; }

        #endregion Commands

        public DeviceControlViewModel(IGenPort genPort)
        {
            GenPort = genPort;

            XmlDocument xml = new XmlDocument();
            xml.Load("GEV_B1020C_v209.xml");
            XmlHelper xmlHelper = new XmlHelper("Category", xml, genPort);
            Categories = xmlHelper.CategoryDictionary;
            LoadedWindowCommand = new DelegateCommand(WindowLoaded);
            ExpandCommand = new DelegateCommand(ExecuteExpandCommand);
            SelectRegisterCommand = new DelegateCommand<CameraRegisterDTO>(ExecuteSelectRegisterCommand);
            // Categories = ReadAllRegisters(Categories);
        }

        public ICategory SelectedCategory
        {
            get => selectedCategory;
            set => selectedCategory = value;
        }

        private void ExecuteSelectRegisterCommand(CameraRegisterDTO cameraRegister)
        {
            SelectedRegister = cameraRegister;
        }

        private void WindowLoaded()
        {
            RaisePropertyChanged(nameof(Categories));
        }

        #region Methods

        /// <summary>
        /// Expand Tree View
        /// </summary>
        private void ExecuteExpandCommand()
        {
            if (IsExpanded)
                IsExpanded = false;
            else
                IsExpanded = true;
        }

        //private List<ICategory> ReadAllRegisters(List<ICategory> categories)
        //{
        //    foreach (var category in categories)
        //    {
        //        if (category == null)
        //            continue;
        //        if (category.PFeatures != null)
        //            category.PFeatures = ReadAllRegisters(category.PFeatures);

        //        if (category.Registers == null)
        //            continue;
        //        if (category.CategoryProperties.Name == "GevTimestampControl")
        //        {
        //        }
        //        if (category.CategoryProperties.Visibility == GenVisibility.Invisible)
        //            continue;

        //        foreach (var registerNode in category.Registers.Values)
        //        {
        //            if (registerNode is IPValue pRegister)
        //            {
        //                if (pRegister is IRegister register)
        //                {
        //                    if (register.AccessMode == GenAccessMode.WO)
        //                        continue;
        //                }

        //                pRegister.GetValue();
        //            }
        //        }
        //    }

        //    return categories;
        //}
    }

    #endregion Methods
}