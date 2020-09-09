using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace GigeVision.Wpf.DTO
{
    public class CameraRegisterGroupDTO
    {
        public ICamera Camera { get; }
        public string Name { get; private set; }
        public object Child { get; private set; }
        public CameraRegister CameraRegister { get; set; }
        public DelegateCommand SetValueCommand { get; set; }
        public bool IsParent { get; set; }

        public CameraRegisterGroupDTO(ICamera camera, string name, object child, bool isParent, CameraRegister cameraRegister = null)
        {
            Camera = camera;
            Name = name;
            Child = child;
            IsParent = isParent;
            CameraRegister = cameraRegister;
            SetValueCommand = new DelegateCommand(WriteValue);
        }

        private async void WriteValue()
        {
            GvcpReply gvcpReply;
            if (CameraRegister.Type == Core.Enums.CameraRegisterType.Integer)
            {
                await Camera.Gvcp.TakeControl(false);
                if (CameraRegister.Value.GetType() == typeof(KeyValuePair<string, int>))
                    gvcpReply = (await Camera.Gvcp.WriteRegisterAsync(CameraRegister.Address, uint.Parse(((KeyValuePair<string, int>)CameraRegister.Value).Value.ToString())));
                else
                    gvcpReply = (await Camera.Gvcp.WriteRegisterAsync(CameraRegister.Address, uint.Parse((string)CameraRegister.Value)));
            }

            if (CameraRegister.Type == Core.Enums.CameraRegisterType.String)
            {
                await Camera.Gvcp.TakeControl(true);

                gvcpReply = (await Camera.Gvcp.WriteMemoryAsync(CameraRegister.Address, BitConverter.ToUInt32(Encoding.ASCII.GetBytes((string)CameraRegister.Value))));
            }
        }
    }
}