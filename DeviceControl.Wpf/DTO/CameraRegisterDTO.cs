using GigeVision.Core.Enums;
using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace DeviceControl.Wpf.DTO
{
    public class CameraRegisterDTO : BindableBase
    {
        public IGvcp Gvcp { get; }
        public CameraRegisterContainer CameraRegisterContainer { get; set; }
        public DelegateCommand<object> SetValueCommand { get; set; }

        public CameraRegisterDTO(IGvcp gvcp, CameraRegisterContainer cameraRegisterContainer)
        {
            Gvcp = gvcp;
            CameraRegisterContainer = cameraRegisterContainer;
            SetValueCommand = new DelegateCommand<object>(WriteValue);
        }

        private async void WriteValue(object newValue)
        {
            GvcpReply gvcpReply = null;
            if (newValue is null)
                return;

            //If Register is ReadOnly Do not Write
            if (CameraRegisterContainer.Register.AccessMode == CameraRegisterAccessMode.RO)
                return;

            switch (CameraRegisterContainer.Type)
            {
                case CameraRegisterType.Integer:
                    //newValue is an Integer
                    if (newValue is Int32 integerValue)
                    {
                        var uIntegreValue = UInt32.Parse($"{integerValue}");
                        if (uIntegreValue == UInt32.Parse($"{ CameraRegisterContainer.Register.Value}"))
                            return;

                        await Gvcp.TakeControl(false);
                        gvcpReply = (await Gvcp.WriteRegisterAsync(CameraRegisterContainer.Register.Address, uIntegreValue));
                    }
                    break;

                case CameraRegisterType.Float:
                    break;

                case CameraRegisterType.StringReg:
                    //newValue is a String
                    if (newValue.Equals(CameraRegisterContainer.Register.Value))
                        return;

                    await Gvcp.TakeControl(false);
                    gvcpReply = (await Gvcp.WriteMemoryAsync(CameraRegisterContainer.Register.Address, BitConverter.ToUInt32(Encoding.ASCII.GetBytes((string)newValue), 0)));

                    break;

                case CameraRegisterType.Enumeration:
                    //newValue is an Enumeration
                    var enumeration = CameraRegisterContainer.TypeValue as Enumeration;

                    await Gvcp.TakeControl(false);
                    gvcpReply = (await Gvcp.WriteRegisterAsync(enumeration.Register.Address, ((KeyValuePair<string, uint>)enumeration.Register.Value).Value));
                    break;

                case CameraRegisterType.Command:
                    //newValue is an UInt32
                    var command = CameraRegisterContainer.TypeValue as CommandRegister;

                    await Gvcp.TakeControl(false);
                    gvcpReply = (await Gvcp.WriteRegisterAsync(command.Register.Address, (uint)command.Register.Value));
                    break;

                case CameraRegisterType.Boolean:
                    //newValue is a boolean
                    var boolean = CameraRegisterContainer.TypeValue as BooleanRegister;

                    await Gvcp.TakeControl(false);
                    gvcpReply = (await Gvcp.WriteRegisterAsync(boolean.Register.Address, (uint)newValue));
                    break;

                default:
                    break;
            }

            //Error Message
            if (gvcpReply.Status == GvcpStatus.GEV_STATUS_SUCCESS)
            {
                CameraRegisterContainer.Register.Value = newValue;
                return;
            }
            MessageBox.Show($"{gvcpReply.Status}");
            RaisePropertyChanged(nameof(CameraRegisterContainer));
        }
    }
}