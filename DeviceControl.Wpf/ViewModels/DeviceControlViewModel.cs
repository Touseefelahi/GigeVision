using GenICam;
using GigeVision.Core;
using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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

        public Int64 ValueToWrite
        {
            get;
            set;
        }

        #endregion Properties

        #region Commands

        public DelegateCommand TestDataTriggerCommand { get; }
        public DelegateCommand ExpandCommand { get; }

        #endregion Commands

        public DeviceControlViewModel(string ip)
        {
            LoadedWindowCommand = new DelegateCommand(WindowLoaded);
            ExpandCommand = new DelegateCommand(ExecuteExpandCommand);

            Gvcp gvcp = new Gvcp(ip);

            Task.Run(async () =>
            {
                await gvcp.ReadAllRegisterAddressFromCameraAsync().ConfigureAwait(false);
                Categories = gvcp.CategoryDictionary;
            });
        }

        public ICategory SelectedCategory
        {
            get => selectedCategory;
            set => selectedCategory = value;
        }

        public IGvcp Gvcp { get; }
        public ICamera Camera { get; }

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
    }

    #endregion Methods
}