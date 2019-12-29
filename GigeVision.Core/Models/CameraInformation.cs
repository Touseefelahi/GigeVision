using GigeVision.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigeVision.Core.Models
{
    public class CameraInformation
    {
        public CameraInformation()
        {
            MacAddress = new byte[6];
        }

        public string IP { get; set; }
        public string MAC { get; set; }
        public byte[] MacAddress { get; set; }
        public string Model { get; set; }
        public string SerialNumber { get; set; }
        public string ManufacturerName { get; set; }
        public string ManufacturerSpecificInformation { get; set; }
        public string Version { get; set; }
        public string UserDefinedName { get; set; }
        public bool IsAvailable { get; set; }
        public CameraStatus Status { get; set; }
    }
}