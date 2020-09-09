using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GigeVision.Core.Models
{
    public class CameraRegisterGroup
    {
        public string Comment { get; private set; }
        public List<string> Category { get; set; }
        //public List<CameraRegister> CameraRegisters { get; set; }

        public CameraRegisterGroup(string comment, List<string> cameraRegisters)
        {
            Comment = comment;
            Category = cameraRegisters;
        }
    }
}