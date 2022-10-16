using GigeVision.Core.Enums;
using GigeVision.Core.Interfaces;
using Stira.WpfCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GigeVision.Core.Models
{
    /// <summary>
    /// Motor Controller for device
    /// </summary>
    public class MotorControl : BaseNotifyPropertyChanged
    {
        private uint zoomValue;

        private uint focusValue;

        private bool hasZoomControl;

        private bool hasFocusControl;

        private bool hasIrisControl;

        private bool hasFixedZoomValue;

        private bool hasFixedFocusValue;

        /// <summary>
        /// Motor Controller for device
        /// </summary>
        public MotorControl()
        {
            LensControl = new Dictionary<LensCommand, string>();
        }

        /// <summary>
        /// Dictionary for motor commands (Lens command)
        /// </summary>
        public Dictionary<LensCommand, string> LensControl { get; private set; }

        /// <summary>
        /// Current Zoom Value
        /// </summary>
        public uint ZoomValue
        {
            get => zoomValue;
            set
            {
                if (zoomValue != value)
                {
                    zoomValue = value;
                    OnPropertyChanged(nameof(ZoomValue));
                }
            }
        }

        /// <summary>
        /// Current Focus Value
        /// </summary>
        public uint FocusValue
        {
            get => focusValue;
            set
            {
                if (focusValue != value)
                {
                    focusValue = value;
                    OnPropertyChanged(nameof(FocusValue));
                }
            }
        }

        /// <summary>
        /// Enables only if it detects that devices has zoom registers
        /// </summary>
        public bool HasZoomControl
        {
            get => hasZoomControl;
            private set
            {
                if (hasZoomControl != value)
                {
                    hasZoomControl = value;
                    OnPropertyChanged(nameof(HasZoomControl));
                }
            }
        }

        /// <summary>
        /// Enables only if it detects that devices has Focus registers
        /// </summary>
        public bool HasFocusControl
        {
            get => hasFocusControl;
            private set
            {
                if (hasFocusControl != value)
                {
                    hasFocusControl = value;
                    OnPropertyChanged(nameof(HasFocusControl));
                }
            }
        }

        /// <summary>
        /// Enables only if it detects that devices has Iris registers
        /// </summary>
        public bool HasIrisControl
        {
            get => hasIrisControl;
            private set
            {
                if (hasIrisControl != value)
                {
                    hasIrisControl = value;
                    OnPropertyChanged(nameof(HasIrisControl));
                }
            }
        }

        /// <summary>
        /// Enables only if it detects that devices has Fixed zoom registers
        /// </summary>
        public bool HasFixedZoomValue
        {
            get => hasFixedZoomValue;
            private set
            {
                if (hasFixedZoomValue != value)
                {
                    hasFixedZoomValue = value;
                    OnPropertyChanged(nameof(HasFixedZoomValue));
                }
            }
        }

        /// <summary>
        /// Enables only if it detects that devices has fixed focus registers
        /// </summary>
        public bool HasFixedFocusValue
        {
            get => hasFixedFocusValue;
            private set
            {
                if (hasFixedFocusValue != value)
                {
                    hasFixedFocusValue = value;
                    OnPropertyChanged(nameof(HasFixedFocusValue));
                }
            }
        }

        internal async Task<bool> SendMotorCommand(IGvcp Gvcp, LensCommand command, uint value = 1)
        {
            if (LensControl.ContainsKey(command))
            {
                if (LensControl.ContainsKey(LensCommand.FocusAuto))
                {
                    switch (command)
                    {
                        case LensCommand.FocusFar:
                        case LensCommand.FocusNear:
                            await Gvcp.WriteRegisterAsync(LensControl[LensCommand.FocusAuto], 0).ConfigureAwait(false);
                            break;
                    }
                }
                bool status = (await Gvcp.WriteRegisterAsync(LensControl[command], value).ConfigureAwait(false)).Status == GvcpStatus.GEV_STATUS_SUCCESS;
                if (LensControl.ContainsKey(LensCommand.FocusAuto))
                {
                    switch (command)
                    {
                        case LensCommand.ZoomStop:
                            await Gvcp.WriteRegisterAsync(LensControl[LensCommand.FocusAuto], 3).ConfigureAwait(false);
                            if (LensControl.ContainsKey(LensCommand.ZoomValue))
                            {
                                GvcpReply zoomValue = await Gvcp.ReadRegisterAsync(LensControl[LensCommand.ZoomValue]).ConfigureAwait(false);
                                if (zoomValue.Status == GvcpStatus.GEV_STATUS_SUCCESS)
                                {
                                    ZoomValue = zoomValue.RegisterValue;
                                }
                            }
                            break;

                        case LensCommand.FocusStop:
                        case LensCommand.FocusValue:
                            if (LensControl.ContainsKey(LensCommand.FocusValue))
                            {
                                GvcpReply focusValue = await Gvcp.ReadRegisterAsync(LensControl[LensCommand.FocusValue]).ConfigureAwait(false);
                                if (focusValue.Status == GvcpStatus.GEV_STATUS_SUCCESS)
                                {
                                    FocusValue = focusValue.RegisterValue;
                                }
                            }
                            break;
                    }
                }
                return status;
            }
            return false;
        }

        internal void CheckMotorControl(Dictionary<string, string> registersDictionary)
        {
            try
            {
                LensControl = new Dictionary<LensCommand, string>();
                string[] take = new string[] { "ZoomIn", "ZoomTele" };
                string[] skip = new string[] { "Step", "Speed", "Limit", "Digital" };
                AddLensRegister(take, skip, LensCommand.ZoomIn, registersDictionary);

                take = new string[] { "ZoomOut", "ZoomWide" };
                if (AddLensRegister(take, skip, LensCommand.ZoomOut, registersDictionary))
                {
                    HasZoomControl = true;
                }
                take = new string[] { "ZoomStop" };
                AddLensRegister(take, skip, LensCommand.ZoomStop, registersDictionary);

                take = new string[] { "ZoomReg" };
                if (AddLensRegister(take, skip, LensCommand.ZoomValue, registersDictionary))
                {
                    HasFixedZoomValue = true;
                }

                take = new string[] { "FocusFar" };
                if (AddLensRegister(take, skip, LensCommand.FocusFar, registersDictionary))
                {
                    HasFocusControl = true;
                }

                take = new string[] { "FocusNear" };
                if (AddLensRegister(take, skip, LensCommand.FocusNear, registersDictionary))
                {
                    HasFocusControl = true;
                }

                take = new string[] { "FocusStop" };
                if (AddLensRegister(take, skip, LensCommand.FocusStop, registersDictionary))
                {
                    HasFocusControl = true;
                }

                take = new string[] { "FocusReg" };
                if (AddLensRegister(take, skip, LensCommand.FocusValue, registersDictionary))
                {
                    HasFixedFocusValue = true;
                }

                take = new string[] { "FocusAuto" };
                AddLensRegister(take, skip, LensCommand.FocusAuto, registersDictionary);

                take = new string[] { "IrisOpen" };
                if (AddLensRegister(take, skip, LensCommand.IrisOpen, registersDictionary))
                {
                    HasIrisControl = true;
                }

                take = new string[] { "IrisClose" };
                if (AddLensRegister(take, skip, LensCommand.IrisClose, registersDictionary))
                {
                    HasIrisControl = true;
                }

                take = new string[] { "IrisStop" };
                if (AddLensRegister(take, skip, LensCommand.IrisStop, registersDictionary))
                {
                    HasIrisControl = true;
                }

                take = new string[] { "AutoIris" };
                if (AddLensRegister(take, skip, LensCommand.IrisAuto, registersDictionary))
                {
                    HasIrisControl = true;
                }
            }
            catch (Exception ex)
            {
            }
        }

        private bool AddLensRegister(string[] lookFor, string[] skipThese, LensCommand lensCommand, Dictionary<string, string> registersDictionary)
        {
            List<string> totalKeys = new();
            foreach (string item in lookFor)
            {
                foreach (string keyItem in registersDictionary.Keys.Where(x => x.Contains(item)))
                {
                    totalKeys.Add(keyItem);
                }
            }
            if (totalKeys?.Count > 0)
            {
                foreach (string skipKey in skipThese)
                {
                    List<string> toBeRemoved = totalKeys.Where(x => x.Contains(skipKey)).ToList();

                    for (int i = 0; i < toBeRemoved.Count; i++)
                    {
                        totalKeys.Remove(toBeRemoved[i]);
                    }
                }
                if (totalKeys.Count > 0)
                {
                    if (!string.IsNullOrEmpty(registersDictionary[totalKeys.FirstOrDefault()]))
                    {
                        LensControl.Add(lensCommand, registersDictionary[totalKeys.FirstOrDefault()]);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}