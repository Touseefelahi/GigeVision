using GenICam;
using GigeVision.Core;
using GigeVision.Core.Enums;
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

        public Dictionary<string, string> RegistersDictionary { get; private set; }

        #region Properties

        /// <summary>
        /// This List is binded to the TreeView as a parent
        /// </summary>
        public List<ICategory> Categories { get; set; }

        public ICommand LoadedWindowCommand { get; }

        public GenVisibility CameraRegisterVisibility { get; set; }

        public CameraStatus CameraStatus { get; set; }

        public bool IsExpanded { get; set; } = false;

        public ICategory SelectedCategory
        {
            get => selectedCategory;
            set => selectedCategory = value;
        }

        public IGvcp Gvcp { get; }
        public ICamera Camera { get; }
        private List<ICategory> CategoryDictionary { get; }

        #endregion Properties

        #region Commands

        public DelegateCommand ExpandCommand { get; }

        #endregion Commands

        public DeviceControlViewModel(string ip)
        {
            LoadedWindowCommand = new DelegateCommand(WindowLoaded);
            ExpandCommand = new DelegateCommand(ExecuteExpandCommand);

            Gvcp = new Gvcp(ip);

            Task.Run(async () =>
            {
                RegistersDictionary = await Gvcp.ReadAllRegisterAddressFromCameraAsync().ConfigureAwait(false);
                Categories = Gvcp.CategoryDictionary;
            });
            CheckControl();
        }

        #region Methods

        private void WindowLoaded()
        {
            RaisePropertyChanged(nameof(Categories));
        }

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

        private async void CheckControl()
        {
            while (true)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                CameraStatus = await Gvcp.CheckCameraStatusAsync().ConfigureAwait(false);
            }
        }

        #endregion Methods
    }
}