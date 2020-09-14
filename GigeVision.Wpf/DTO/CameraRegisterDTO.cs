using GigeVision.Core.Enums;
using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace GigeVision.Wpf.DTO
{
    public class CameraRegisterDTO
    {
        public ICamera Camera { get; }
        public CameraRegister CameraRegister { get; set; }
        public DelegateCommand SetValueCommand { get; set; }

        public CameraRegisterDTO(ICamera camera, CameraRegister cameraRegister)
        {
            Camera = camera;
            CameraRegister = cameraRegister;
            SetValueCommand = new DelegateCommand(WriteValue);
        }

        private async void WriteValue()
        {
            GvcpReply gvcpReply;
            if (CameraRegister.Type == CameraRegisterType.Integer)
            {
                await Camera.Gvcp.TakeControl(false);
                if (CameraRegister.Value is KeyValuePair<string, uint> dictionaryValue)
                    gvcpReply = (await Camera.Gvcp.WriteRegisterAsync(CameraRegister.Address, dictionaryValue.Value));
                else if (CameraRegister.Value is string stringValue)
                    gvcpReply = (await Camera.Gvcp.WriteRegisterAsync(CameraRegister.Address, uint.Parse(stringValue)));
                else if (CameraRegister.Value is uint uintValue)
                    gvcpReply = (await Camera.Gvcp.WriteRegisterAsync(CameraRegister.Address, uintValue));
            }

            if (CameraRegister.Type == CameraRegisterType.String)
            {
                await Camera.Gvcp.TakeControl(true);

                gvcpReply = (await Camera.Gvcp.WriteMemoryAsync(CameraRegister.Address, BitConverter.ToUInt32(Encoding.ASCII.GetBytes((string)CameraRegister.Value), 0)));
            }
        }
    }
}