using GigeVision.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace GigeVision.Core.Models
{
    public class CameraRegister
    {
        public string Name { get; private set; }

        public string? Descrption { get; private set; }
        public CameraRegisterVisibilty? Visibilty { get; private set; }
        public string Address { get; private set; }
        public uint Length { get; private set; }
        public CameraRegisterAccessMode AccessMode { get; private set; }
        public bool IsStreamable { get; private set; }
        public CameraRegisterType Type { get; private set; }

        public Dictionary<string, int> Enumeration { get; private set; }

        /// <summary>
        /// Value could be string, int or boolean
        /// </summary>
        public Object Value { get; set; }

        public CameraRegister(string registerName, string? description, CameraRegisterVisibilty? registerVisibilty, string address, uint length, CameraRegisterAccessMode registerAccessMode,
            bool isStreamable, CameraRegisterType registerType, Dictionary<string, int> registerEnumeration = null)
        {
            Name = registerName;
            Descrption = description;
            Visibilty = registerVisibilty;
            Address = address;
            Length = length;
            AccessMode = registerAccessMode;
            IsStreamable = isStreamable;
            Type = registerType;
            Enumeration = registerEnumeration;
        }
    }
}