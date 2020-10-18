using GigeVision.Core.Enums;
using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace GigeVision.Wpf.DTO
{
    public class CameraRegisterDTO
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

            if (CameraRegisterContainer.Register.AccessMode == CameraRegisterAccessMode.RO)
                return;

            if (newValue != CameraRegisterContainer.Register.Value)
            {
                await Gvcp.TakeControl(false);
                switch (CameraRegisterContainer.Type)
                {
                    case CameraRegisterType.Integer:
                        var integerRegister = CameraRegisterContainer.TypeValue as IntegerRegister;
                        try
                        {
                            gvcpReply = (await Gvcp.WriteRegisterAsync(integerRegister.Register.Address, uint.Parse((string)newValue)));
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error {ex}");
                        }
                        break;

                    case CameraRegisterType.Float:
                        break;

                    case CameraRegisterType.StringReg:
                        var stringRegister = CameraRegisterContainer.TypeValue as CameraRegister;
                        //gvcpReply = (await Camera.Gvcp.WriteRegisterAsync(stringRegister.Address, uint.Parse((string)stringRegister.Value)));
                        gvcpReply = (await Gvcp.WriteMemoryAsync(stringRegister.Address, BitConverter.ToUInt32(Encoding.ASCII.GetBytes((string)stringRegister.Value), 0)));
                        break;

                    case CameraRegisterType.Enumeration:
                        var enumeration = CameraRegisterContainer.TypeValue as Enumeration;
                        gvcpReply = (await Gvcp.WriteRegisterAsync(enumeration.Register.Address, ((KeyValuePair<string, uint>)enumeration.Register.Value).Value));

                        break;

                    case CameraRegisterType.Command:
                        var command = CameraRegisterContainer.TypeValue as CommandRegister;
                        gvcpReply = (await Gvcp.WriteRegisterAsync(command.Register.Address, (uint)command.Register.Value));
                        break;

                    case CameraRegisterType.Boolean:
                        var boolean = CameraRegisterContainer.TypeValue as BooleanRegister;
                        gvcpReply = (await Gvcp.WriteRegisterAsync(boolean.Register.Address, (uint)newValue));
                        break;

                    default:
                        break;
                }
                if (gvcpReply.Status != GvcpStatus.GEV_STATUS_SUCCESS)
                {
                    MessageBox.Show($"{gvcpReply.Status}");
                }
            }
        }
    }
}