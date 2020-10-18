using GigeVision.Core.Enums;

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
        /// Reister Length
        /// </summary>
        public uint Length { get; private set; }

        /// <summary>
        /// Register Access Mode
        /// </summary>
        public CameraRegisterAccessMode AccessMode { get; private set; }

        /// <summary>
        /// Register Value
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Main Method
        /// </summary>
        /// <param name="address"></param>
        /// <param name="length"></param>
        /// <param name="registerAccessMode"></param>
        /// <param name="value"></param>
        /// <param name="addressParameter"></param>
        public CameraRegister(string? address, uint length, CameraRegisterAccessMode registerAccessMode, object value = null, IntSwissKnife addressParameter = null)
        {
            Address = address;
            Length = length;
            AccessMode = registerAccessMode;
            AddressParameter = addressParameter;
        }
    }
}