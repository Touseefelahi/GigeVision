using GigeVision.Core.Enums;

namespace GigeVision.Core.Models
{
    /// <summary>
    /// Discovery Packet Information for GigeCamera
    /// </summary>
    public class CameraInformation
    {
        /// <summary>
        /// Discovery Packet Information for GigeCamera
        /// </summary>
        public CameraInformation()
        {
            MacAddress = new byte[6];
        }

        /// <summary>
        /// Camera IP
        /// </summary>
        public string IP { get; set; }

        /// <summary>
        /// Camera MAC address
        /// </summary>
        public string MAC { get; set; }

        /// <summary>
        /// Camera MAC address
        /// </summary>
        public byte[] MacAddress { get; set; }

        /// <summary>
        /// Camera Model
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Device Serial number
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// Device Manufacturer Name
        /// </summary>
        public string ManufacturerName { get; set; }

        /// <summary>
        /// Device Manufactuere Specific Information
        /// </summary>
        public string ManufacturerSpecificInformation { get; set; }

        /// <summary>
        /// Device Version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Device User Defined Name
        /// </summary>
        public string UserDefinedName { get; set; }

        /// <summary>
        /// Device Status
        /// </summary>
        public CameraStatus Status { get; set; }
    }
}