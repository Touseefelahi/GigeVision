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
        //public IGvcp Gvcp { get; }
        //public CameraRegisterContainer CameraRegisterContainer { get; set; }
        //public DelegateCommand<object> SetValueCommand { get; set; }

        //public CameraRegisterDTO(IGvcp gvcp, CameraRegisterContainer cameraRegisterContainer)
        //{
        //    Gvcp = gvcp;
        //    CameraRegisterContainer = cameraRegisterContainer;
        //    SetValueCommand = new DelegateCommand<object>(WriteValue);
        //}

        //private async void WriteValue(object newValue)
        //{
        //    return;

        //    //If Register is ReadOnly Do not Write
        //    if (CameraRegisterContainer.Register.AccessMode == CameraRegisterAccessMode.RO)
        //        return;

        //    GvcpReply gvcpReply = null;
        //    switch (CameraRegisterContainer.Type)
        //    {
        //        case CameraRegisterType.Integer:
        //            //newValue is an Integer
        //            if (newValue is Int32 integerValue)
        //            {
        //                var uIntegreValue = UInt32.Parse($"{integerValue}");
        //                if (uIntegreValue == UInt32.Parse($"{ CameraRegisterContainer.Value}"))
        //                    return;

        //                await Gvcp.TakeControl(false);
        //                gvcpReply = (await Gvcp.WriteRegisterAsync(CameraRegisterContainer.Register.Address, uIntegreValue));
        //            }
        //            break;

        //        case CameraRegisterType.Float:
        //            break;

        //        case CameraRegisterType.StringReg:

        //            if (newValue is null)
        //                return;

        //            //newValue is a String
        //            if (newValue.Equals(CameraRegisterContainer.Value))
        //                return;

        //            await Gvcp.TakeControl(false);
        //            gvcpReply = (await Gvcp.WriteMemoryAsync(CameraRegisterContainer.Register.Address, BitConverter.ToUInt32(Encoding.ASCII.GetBytes((string)newValue), 0)));
        //            break;

        //        case CameraRegisterType.Enumeration:
        //            //newValue is an Enumeration
        //            var enumeration = CameraRegisterContainer.TypeValue as Enumeration;
        //            newValue = ((KeyValuePair<string, uint>)enumeration.Value).Value;

        //            if (newValue is null)
        //                return;

        //            await Gvcp.TakeControl(false);
        //            gvcpReply = await Gvcp.WriteRegisterAsync(CameraRegisterContainer.Register.Address, (uint)newValue);
        //            break;

        //        case CameraRegisterType.Command:
        //            //newValue is an UInt32
        //            newValue = CameraRegisterContainer.Value;
        //            var command = CameraRegisterContainer.TypeValue as CommandRegister;

        //            if (newValue is null)
        //                return;

        //            await Gvcp.TakeControl(false);
        //            gvcpReply = (await Gvcp.WriteRegisterAsync(command.Register.Address, (uint)newValue));
        //            break;

        //        case CameraRegisterType.Boolean:
        //            //newValue is a boolean
        //            var boolean = CameraRegisterContainer.TypeValue as BooleanRegister;

        //            if (newValue is null)
        //                return;

        //            await Gvcp.TakeControl(false);
        //            gvcpReply = (await Gvcp.WriteRegisterAsync(boolean.Register.Address, (uint)newValue));
        //            break;

        //        default:
        //            break;
        //    }

        //    //Error Message
        //    if (gvcpReply.Status == GvcpStatus.GEV_STATUS_SUCCESS)
        //    {
        //        CameraRegisterContainer.Value = newValue;
        //        return;
        //    }
        //    MessageBox.Show($"{gvcpReply.Status}");
        //    RaisePropertyChanged(nameof(CameraRegisterContainer));
        //}
    }
}