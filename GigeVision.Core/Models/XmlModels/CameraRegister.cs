using GigeVision.Core.Enums;
using System.Net.Sockets;

namespace GigeVision.Core.Models
{
    /// <summary>
    /// Camera Register is used in all Register types to hold their registers' information;
    /// </summary>
    public class CameraRegister
    {
        /// <summary>
        /// Register Address in hex format
        /// </summary>
        public string? Address { get; private set; }

        /// <summary>
        /// Address Parameter
        /// </summary>
        public IntSwissKnife AddressParameter { get; set; }

        /// <summary>
        /// Register Length
        /// </summary>
        public ushort Length { get; private set; }

        /// <summary>
        /// Register Access Mode
        /// </summary>
        public CameraRegisterAccessMode AccessMode { get; set; }

        /// <summary>
        /// Main Method
        /// </summary>
        /// <param name="address"></param>
        /// <param name="length"></param>
        /// <param name="registerAccessMode"></param>
        /// <param name="value"></param>
        /// <param name="addressParameter"></param>
        public CameraRegister(string? address, ushort length, CameraRegisterAccessMode registerAccessMode, IntSwissKnife addressParameter = null)
        {
            if (address is null && addressParameter != null)
            {
                if (addressParameter.Value != null)
                    Address = addressParameter.Value.ToString();
            }
            else
                Address = address;
            Length = length;
            AccessMode = registerAccessMode;
            AddressParameter = addressParameter;
        }
    }
}