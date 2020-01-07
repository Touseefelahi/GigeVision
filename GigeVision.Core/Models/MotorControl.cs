using GigeVision.Core.Enums;
using GigeVision.Core.Interfaces;
using Stira.WpfCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigeVision.Core.Models
{
    public class MotorControl : BaseNotifyPropertyChanged
    {
        private uint zoomValue;

        private uint focusValue;

        private bool hasZoomControl;

        private bool hasFocusControl;

        private bool hasIrisControl;

        private bool hasFixedZoomValue;

        private bool hasFixedFocusValue;

        public MotorControl()
        {
            LensControl = new Dictionary<LensCommand, string>();
        }

        public Dictionary<LensCommand, string> LensControl { get; private set; }

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

        public uint FocusValue
        {
            get { return focusValue; }
            set
            {
                if (focusValue != value)
                {
                    focusValue = value;
                    OnPropertyChanged(nameof(FocusValue));
                }
            }
        }

        public bool HasZoomControl
        {
            get { return hasZoomControl; }
            set
            {
                if (hasZoomControl != value)
                {
                    hasZoomControl = value;
                    OnPropertyChanged(nameof(HasZoomControl));
                }
            }
        }

        public bool HasFocusControl
        {
            get { return hasFocusControl; }
            set
            {
                if (hasFocusControl != value)
                {
                    hasFocusControl = value;
                    OnPropertyChanged(nameof(HasFocusControl));
                }
            }
        }

        public bool HasIrisControl
        {
            get { return hasIrisControl; }
            set
            {
                if (hasIrisControl != value)
                {
                    hasIrisControl = value;
                    OnPropertyChanged(nameof(HasIrisControl));
                }
            }
        }

        public bool HasFixedZoomValue
        {
            get { return hasFixedZoomValue; }
            set
            {
                if (hasFixedZoomValue != value)
                {
                    hasFixedZoomValue = value;
                    OnPropertyChanged(nameof(HasFixedZoomValue));
                }
            }
        }

        public bool HasFixedFocusValue
        {
            get { return hasFixedFocusValue; }
            set
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
                var status = (await Gvcp.WriteRegisterAsync(LensControl[command], value).ConfigureAwait(false)).Status == GvcpStatus.GEV_STATUS_SUCCESS;
                if (LensControl.ContainsKey(LensCommand.FocusAuto))
                {
                    switch (command)
                    {
                        case LensCommand.ZoomStop:
                            await Gvcp.WriteRegisterAsync(LensControl[LensCommand.FocusAuto], 3).ConfigureAwait(false);
                            if (LensControl.ContainsKey(LensCommand.ZoomValue))
                            {
                                var zoomValue = await Gvcp.ReadRegisterAsync(LensControl[LensCommand.ZoomValue]).ConfigureAwait(false);
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
                                var focusValue = await Gvcp.ReadRegisterAsync(LensControl[LensCommand.FocusValue]).ConfigureAwait(false);
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
                var take = new string[] { "ZoomIn", "ZoomTele" };
                var skip = new string[] { "Step", "Speed", "Limit", "Digital" };
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
            List<string> totalKeys = new List<string>();
            foreach (var item in lookFor)
            {
                var keys = registersDictionary.Keys.Where(x => x.Contains(item));
                foreach (var keyItem in keys)
                {
                    totalKeys.Add(keyItem);
                }
            }
            if (totalKeys?.Count() > 0)
            {
                foreach (var skipKey in skipThese)
                {
                    var toBeRemoved = totalKeys.Where(x => x.Contains(skipKey)).ToList();

                    foreach (var item in toBeRemoved)
                    {
                        totalKeys.Remove(item);
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