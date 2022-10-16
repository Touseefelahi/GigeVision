using GigeVision.Core.Enums;
using Stira.WpfCore;

namespace GigeVision.Core.Models
{
    /// <summary>
    /// Discovery Packet Information for GigeCamera
    /// </summary>
    public class CameraInformation : BaseNotifyPropertyChanged
    {
        private string iP, networkIP;
        private CameraStatus status;

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
        public string IP
        {
            get { return iP; }
            set
            {
                if (iP != value)
                {
                    iP = value;
                    OnPropertyChanged(nameof(IP));
                }
            }
        }

        /// <summary>
        /// Camera MAC address
        /// </summary>
        public string MAC { get; set; }

        /// <summary>
        /// Camera MAC address
        /// </summary>
        public byte[] MacAddress { get; set; }

        /// <summary>
        /// Device Manufacturer Name
        /// </summary>
        public string ManufacturerName { get; set; }

        /// <summary>
        /// Device Manufacture Specific Information
        /// </summary>
        public string ManufacturerSpecificInformation { get; set; }

        /// <summary>
        /// Camera Model
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Network IP
        /// </summary>
        public string NetworkIP
        {
            get { return networkIP; }
            set
            {
                if (networkIP != value)
                {
                    networkIP = value;
                    OnPropertyChanged(nameof(NetworkIP));
                }
            }
        }

        /// <summary>
        /// Device Serial number
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// Device Status
        /// </summary>
        public CameraStatus Status
        {
            get { return status; }
            set
            {
                if (status != value)
                {
                    status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        /// <summary>
        /// Device User Defined Name
        /// </summary>
        public string UserDefinedName { get; set; }

        /// <summary>
        /// Device Version
        /// </summary>
        public string Version { get; set; }

        public override string ToString()
        {
            return IP;
        }
    }
}