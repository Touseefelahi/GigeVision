using GigeVision.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace GigeVision.Core.Models
{
    public class CameraRegister
    {
        public string Name { get; private set; }
        public string Comment { get; private set; }
        public string? Descrption { get; private set; }
        public CameraRegisterVisibilty? Visibilty { get; private set; }
        public string Address { get; private set; }
        public uint Length { get; private set; }
        public CameraRegisterAccessMode AccessMode { get; private set; }
        public bool IsStreamable { get; private set; }
        public CameraRegisterType Type { get; private set; }

        public CameraRegister(string registerName, string? description, CameraRegisterVisibilty? registerVisibilty, string address, uint length, CameraRegisterAccessMode registerAccessMode,
            bool isStreamable, CameraRegisterType registerType, string comment)
        {
            Name = registerName;
            Descrption = description;
            Visibilty = registerVisibilty;
            Address = address;
            Length = length;
            AccessMode = registerAccessMode;
            IsStreamable = isStreamable;
            Type = registerType;
            Comment = comment;
        }
    }
}