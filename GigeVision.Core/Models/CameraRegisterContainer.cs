using GigeVision.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace GigeVision.Core.Models
{
    public class CameraRegisterContainer
    {
        public string Name { get; set; }
        public string Descrption { get; private set; }
        public CameraRegisterVisibilty Visibilty { get; private set; }
        public bool IsStreamable { get; private set; }
        public CameraRegisterType Type { get; private set; }
        public object TypeValue { get; private set; }

        public CameraRegisterContainer(string registerName, string description, CameraRegisterVisibilty registerVisibilty, bool isStreamable, CameraRegisterType registerType,
             object typeValue = null)
        {
            Name = registerName;
            Descrption = description;
            Visibilty = registerVisibilty;
            IsStreamable = isStreamable;
            Type = registerType;
            TypeValue = typeValue;
        }
    }
}