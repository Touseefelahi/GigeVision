using DeviceControl.Wpf.DTO;
using GigeVision.Core.Enums;
using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DeviceControl.Wpf.ViewModels
{
    public class DeviceControlViewModel : BindableBase
    {
        #region Properties

        /// <summary>
        /// This List is binded to the TreeView as a parent
        /// </summary>
        public ObservableCollection<CameraRegisterGroupDTO> CameraRegisterGroupDTOList { get; set; }

        public Dictionary<string, CameraRegisterContainer> FilteredRegistersDictionary { get; set; }
        private List<CameraRegisterDTO> CameraRegistersList { get; set; }

        public IGvcp Gvcp { get; }
        public ICommand LoadedWindowCommand { get; }

        public CameraRegisterVisibility CameraRegisterVisibility
        {
            get;
            set;
        }

        public bool IsBusy { get; set; }

        public bool IsExpanded { get; set; } = true;

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

        public DeviceControlViewModel(IGvcp gvcp)
        {
            Gvcp = gvcp;
            Task.Run(async () => await Gvcp.ReadAllRegisterAddressFromCameraAsync().ConfigureAwait(false));
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
        /// this method reads all device control registers values
        /// </summary>
        private async Task ReadDeviceControlRegisters()
        {
            var chunkSize = 30;
            var skipSize = 0;
            FilteredRegistersDictionary = Gvcp.RegistersDictionary.Where(x => x.Value.Register != null && x.Value.Visibility != CameraRegisterVisibility.Invisible).ToDictionary(x => x.Key, x => x.Value);
            var readableRegisters = FilteredRegistersDictionary.Where(x => x.Value.Register.Address != null && x.Value.Register.AccessMode != CameraRegisterAccessMode.WO).ToDictionary(x => x.Key, x => x.Value);
            while (readableRegisters.Count > skipSize)
            {
                var packetOfRegisters = readableRegisters.Skip(skipSize).Take(chunkSize).Select(x => x.Value.Register.Address).ToList();
                var values = (await Gvcp.ReadRegisterAsync(packetOfRegisters.ToArray()));
                if (values.Status == GvcpStatus.GEV_STATUS_SUCCESS)
                {
                    int index = 0;
                    foreach (var register in readableRegisters.Skip(skipSize).Take(chunkSize).Select(x => x.Key))
                    {
                        Gvcp.RegistersDictionary[register].Register.Value = values.RegisterValues[index];
                        index++;
                    }

                    skipSize += chunkSize;
                }
            }
        }

        public async void CreateCameraRegistersGroupCollection()
        {
            //var test1 = await Camera.Gvcp.ReadRegisterAsync(Camera.Gvcp.RegistersDictionary["Width"]);
            //var test2 = await Camera.Gvcp.ReadRegisterAsync(Camera.Gvcp.RegistersDictionary["TimerGranularityFactor"]);
            await ReadDeviceControlRegisters();
            CameraRegistersList = new List<CameraRegisterDTO>();

            CameraRegisterGroupDTOList = new ObservableCollection<CameraRegisterGroupDTO>();
            foreach (var categoryFeature in Gvcp.RegistersGroupDictionary["Root"].Category)
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
        /// Helper method to reorganize camera registers structure
        /// </summary>
        /// <param name="categoryFeature"></param>
        /// <returns></returns>

        private async Task<CameraRegisterGroupDTO> GetChild(string categoryFeature)
        {
            CameraRegisterContainer cameraRegisterContainer = null;
            ObservableCollection<CameraRegisterGroupDTO> cameraRegisterGroupDTOs = new ObservableCollection<CameraRegisterGroupDTO>();

            //Look for parent (group)
            if (Gvcp.RegistersGroupDictionary.ContainsKey(categoryFeature))
            {
                foreach (var feature in Gvcp.RegistersGroupDictionary[categoryFeature].Category)
                {
                    //When you find it look for its` children
                    //child might be either parent of other children or child
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
                if (Gvcp.RegistersGroupDictionary["Root"].Category.Contains(categoryFeature))
                    return new CameraRegisterGroupDTO(Gvcp, categoryFeature, cameraRegisterGroupDTOs);

                return new CameraRegisterGroupDTO(Gvcp, categoryFeature, cameraRegisterGroupDTOs);
            }
            else
            {
                //Look for Child (register)
                var isNull = true;
                if (FilteredRegistersDictionary.ContainsKey(categoryFeature))
                {
                    try
                    {
                        if (FilteredRegistersDictionary[categoryFeature].Type == CameraRegisterType.StringReg)
                            FilteredRegistersDictionary[categoryFeature].Register.Value = Encoding.ASCII.GetString((await Gvcp.ReadMemoryAsync(FilteredRegistersDictionary[categoryFeature].Register.Address)).MemoryValue);

                        cameraRegisterContainer = FilteredRegistersDictionary[categoryFeature];
                        isNull = false;
                    }
                    catch
                    {
                    }
                }

                if (isNull)
                    return null;

                CameraRegistersList.Add(new CameraRegisterDTO(Gvcp, cameraRegisterContainer));

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
        }

        #endregion Methods
    }
}