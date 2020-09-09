using GigeVision.Core.Enums;
using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using GigeVision.Wpf.DTO;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace GigeVision.Wpf.ViewModels
{
    public class DeviceControlViewModel : BindableBase
    {
        #region Properties

        private ObservableCollection<CameraRegisterGroupDTO> cameraRegisterGroupDTOList;

        public ObservableCollection<CameraRegisterGroupDTO> CameraRegisterGroupDTOList
        {
            get => cameraRegisterGroupDTOList;
            set
            {
                cameraRegisterGroupDTOList = value;
                SetProperty(ref cameraRegisterGroupDTOList, value);
            }
        }

        private Dictionary<string, CameraRegister> filteredRegistersDictionary;
        public Dictionary<string, CameraRegister> FilteredRegistersDictionary { get => filteredRegistersDictionary; set => filteredRegistersDictionary = value; }

        public ICamera Camera { get; }
        public ICommand LoadedWindowCommand { get; }

        private CameraRegisterVisibilty cameraRegisterVisibilty;

        public CameraRegisterVisibilty CameraRegisterVisibilty
        {
            get
            {
                return cameraRegisterVisibilty;
            }
            set
            {
                cameraRegisterVisibilty = value;
            }
        }

        public bool IsBusy { get; set; }

        private bool isExpanded = false;
        public bool IsExpanded { get { return isExpanded; } set { IsExpanded = value; SetProperty(ref isExpanded, value); } }

        #endregion Properties

        #region Commands

        public DelegateCommand TestDataTriggerCommand { get; }
        public DelegateCommand ExpandCommand { get; }

        #endregion Commands

        public DeviceControlViewModel(ICamera camera)
        {
            Camera = camera;
            LoadedWindowCommand = new DelegateCommand(WindowLoaded);
            TestDataTriggerCommand = new DelegateCommand(CreateCameraRegistersGroupCollection);
            ExpandCommand = new DelegateCommand(ExecuteExpandCommand);
        }

        private void WindowLoaded()
        {
            CreateCameraRegistersGroupCollection();
            RaisePropertyChanged(nameof(CameraRegisterGroupDTOList));
        }

        #region Methods

        private async void CreateCameraRegistersGroupCollection()
        {
            await ReadDeviceControlRegisters();

            cameraRegisterGroupDTOList = new ObservableCollection<CameraRegisterGroupDTO>();

            foreach (var groupName in Camera.Gvcp.RegistersGroupDictionary["Root"].Category)
            {
                var child = await GetChild(groupName);
                if (child != null)
                    cameraRegisterGroupDTOList.Add(child);
            }
        }

        /// <summary>
        /// this method reads all device control registres values
        /// </summary>
        private async Task ReadDeviceControlRegisters()
        {
            var chunkSize = 100;
            var skipSize = 0;
            FilteredRegistersDictionary = Camera.Gvcp.RegistersDictionary.Where(x => x.Value.AccessMode != CameraRegisterAccessMode.WO && x.Value.IsStreamable).ToDictionary(x => x.Key, x => x.Value);

            while (FilteredRegistersDictionary.Count > skipSize)
            {
                var packetOfRegisters = FilteredRegistersDictionary.Skip(skipSize).Take(chunkSize).Select(x => x.Value.Address).ToList();
                var values = (await Camera.Gvcp.ReadRegisterAsync(packetOfRegisters.ToArray()));
                if (values.Status == GvcpStatus.GEV_STATUS_SUCCESS)
                {
                    int index = 0;
                    foreach (var register in FilteredRegistersDictionary.Skip(skipSize).Take(chunkSize).Select(x => x.Key))
                    {
                        Camera.Gvcp.RegistersDictionary[register].Value = values.RegisterValues[index];
                        index++;
                    }

                    skipSize += chunkSize;
                }
            }
        }

        /// <summary>
        /// Helper method to reorgnaize camera registers structure
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        private async Task<CameraRegisterGroupDTO> GetChild(string group)
        {
            CameraRegister cameraRegister = null;
            List<CameraRegister> cameraRegisters = new List<CameraRegister>();
            List<CameraRegisterGroupDTO> cameraRegisterGroupDTOs = new List<CameraRegisterGroupDTO>();

            if (Camera.Gvcp.RegistersGroupDictionary.ContainsKey(group))
            {
                foreach (var feature in Camera.Gvcp.RegistersGroupDictionary[group].Category)
                {
                    var child = await GetChild(feature);
                    if (child != null)
                        cameraRegisterGroupDTOs.Add(child);
                }

                return new CameraRegisterGroupDTO(Camera, group, cameraRegisterGroupDTOs, true);
            }
            else
            {
                var isNull = true;
                if (FilteredRegistersDictionary.ContainsKey(group))
                {
                    try
                    {
                        if (FilteredRegistersDictionary[group].Type == CameraRegisterType.String)
                        {
                            FilteredRegistersDictionary[group].Value = Encoding.ASCII.GetString((await Camera.Gvcp.ReadMemoryAsync(Camera.Gvcp.RegistersDictionary[group].Address)).MemoryValue);
                            cameraRegister = Camera.Gvcp.RegistersDictionary[group];
                            isNull = false;
                        }
                    }
                    catch
                    {
                    }
                }

                if (FilteredRegistersDictionary.ContainsKey($"{group}Reg"))
                {
                    try
                    {
                        cameraRegister = FilteredRegistersDictionary[$"{group}Reg"];
                        isNull = false;
                    }
                    catch
                    {
                    }
                }

                if (isNull)
                    return null;

                cameraRegisters.Add(cameraRegister);
                return new CameraRegisterGroupDTO(Camera, group, cameraRegisterGroupDTOs, false, cameraRegister);
            }
        }

        private void ExecuteExpandCommand()
        {
            if (IsExpanded)
                IsExpanded = false;
            else
                IsExpanded = true;

            RaisePropertyChanged(nameof(IsExpanded));
        }

        #endregion Methods
    }
}