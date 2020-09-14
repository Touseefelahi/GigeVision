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

        public ObservableCollection<CameraRegisterGroupDTO> CameraRegisterGroupDTOList { get; set; }
        public Dictionary<string, CameraRegister> FilteredRegistersDictionary { get; set; }
        private List<CameraRegisterDTO> CameraRegistersList { get; set; }

        public ICamera Camera { get; }
        public ICommand LoadedWindowCommand { get; }

        public CameraRegisterVisibilty CameraRegisterVisibilty
        {
            get;
            set;
        }

        public bool IsBusy { get; set; }

        public bool IsExpanded { get; set; }

        public CameraRegisterDTO SelectedRegister
        {
            get;
            set;
        }

        #endregion Properties

        #region Commands

        public DelegateCommand TestDataTriggerCommand { get; }
        public DelegateCommand ExpandCommand { get; }
        public DelegateCommand<CameraRegisterDTO> SelectRegisterCommand { get; }

        #endregion Commands

        public DeviceControlViewModel(ICamera camera)
        {
            Camera = camera;
            LoadedWindowCommand = new DelegateCommand(WindowLoaded);
            TestDataTriggerCommand = new DelegateCommand(CreateCameraRegistersGroupCollection);
            ExpandCommand = new DelegateCommand(ExecuteExpandCommand);
            SelectRegisterCommand = new DelegateCommand<CameraRegisterDTO>(ExecuteSelectRegisterCommand);
        }

        private void ExecuteSelectRegisterCommand(CameraRegisterDTO cameraRegister)
        {
            SelectedRegister = cameraRegister;
        }

        private void WindowLoaded()
        {
            CreateCameraRegistersGroupCollection();
            RaisePropertyChanged(nameof(CameraRegisterGroupDTOList));
        }

        #region Methods

        /// <summary>
        /// this method reads all device control registres values
        /// </summary>
        private async Task ReadDeviceControlRegisters()
        {
            var chunkSize = 100;
            var skipSize = 0;
            FilteredRegistersDictionary = Camera.Gvcp.RegistersDictionary;

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

        private async void CreateCameraRegistersGroupCollection()
        {
            await ReadDeviceControlRegisters();
            CameraRegistersList = new List<CameraRegisterDTO>();

            CameraRegisterGroupDTOList = new ObservableCollection<CameraRegisterGroupDTO>();
            foreach (var categoryFeature in Camera.Gvcp.RegistersGroupDictionary["Root"].Category)
            {
                var child = await GetChild(categoryFeature);
                if (child != null)
                {
                    if (child.Child != null)
                    {
                        if (child.Child.Count == 0)
                            child.Child = null;
                    }

                    child.CameraRegisters = CameraRegistersList;

                    CameraRegisterGroupDTOList.Add(child);
                    CameraRegistersList = new List<CameraRegisterDTO>();
                }
            }
        }

        /// <summary>
        /// Helper method to reorgnaize camera registers structure
        /// </summary>
        /// <param name="categoryFeature"></param>
        /// <returns></returns>

        private async Task<CameraRegisterGroupDTO> GetChild(string categoryFeature)
        {
            CameraRegister cameraRegister = null;
            ObservableCollection<CameraRegisterGroupDTO> cameraRegisterGroupDTOs = new ObservableCollection<CameraRegisterGroupDTO>();

            //Look for parent (group)
            if (Camera.Gvcp.RegistersGroupDictionary.ContainsKey(categoryFeature))
            {
                foreach (var feature in Camera.Gvcp.RegistersGroupDictionary[categoryFeature].Category)
                {
                    var x = "null";
                    if (feature.Equals("LensType"))
                        x = feature;

                    //When you find it look for its` children
                    //child might be ethier parent of other children or child
                    var child = await GetChild(feature);

                    //check if child is a parent
                    if (child != null)
                    {
                        if (child.Child.Count == 0)
                        {
                            child.Child = null;
                        }
                        if (CameraRegistersList.Count > 0)
                        {
                            child.CameraRegisters = CameraRegistersList;

                            cameraRegisterGroupDTOs.Add(child);
                            CameraRegistersList = new List<CameraRegisterDTO>();
                        }
                        else
                        {
                            cameraRegisterGroupDTOs.Add(child);
                        }
                    }
                }
                //return parent
                if (Camera.Gvcp.RegistersGroupDictionary["Root"].Category.Contains(categoryFeature))
                    return new CameraRegisterGroupDTO(Camera, categoryFeature, cameraRegisterGroupDTOs);

                return new CameraRegisterGroupDTO(Camera, categoryFeature, cameraRegisterGroupDTOs);
            }
            else
            {
                //Look for Child (register)
                var isNull = true;
                if (FilteredRegistersDictionary.ContainsKey(categoryFeature))
                {
                    try
                    {
                        //Some of registers have (Reg) as a postfix and some of them have not
                        //Check first if register name is without postfix
                        // If it exists hold it
                        if (FilteredRegistersDictionary[categoryFeature].Type == CameraRegisterType.String)
                        {
                            FilteredRegistersDictionary[categoryFeature].Value = Encoding.ASCII.GetString((await Camera.Gvcp.ReadMemoryAsync(Camera.Gvcp.RegistersDictionary[categoryFeature].Address)).MemoryValue);
                            cameraRegister = Camera.Gvcp.RegistersDictionary[categoryFeature];
                            isNull = false;
                        }
                    }
                    catch
                    {
                    }
                }

                //Then check if register name is with a postfix
                // If it exists replace it
                if (FilteredRegistersDictionary.ContainsKey($"{categoryFeature}Reg"))
                {
                    try
                    {
                        cameraRegister = FilteredRegistersDictionary[$"{categoryFeature}Reg"];
                        cameraRegister.Name = cameraRegister.Name.Replace("Reg", "");
                        isNull = false;
                    }
                    catch
                    {
                    }
                }

                if (isNull)
                    return null;

                CameraRegistersList.Add(new CameraRegisterDTO(Camera, cameraRegister));

                return null;
            }
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

            RaisePropertyChanged(nameof(IsExpanded));
        }

        #endregion Methods
    }
}