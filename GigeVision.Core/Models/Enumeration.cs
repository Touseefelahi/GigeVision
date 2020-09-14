using System;
using System.Collections.Generic;
using System.Text;

namespace GigeVision.Core.Models
{
    public class Enumeration
    {
        public Dictionary<string, uint> Entry { get; private set; }
        public CameraRegister Register { get; private set; }

        public Enumeration(Dictionary<string, uint> entry, CameraRegister register)
        {
            Entry = entry;
            Register = register;
        }
    }
}