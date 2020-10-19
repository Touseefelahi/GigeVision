using GigeVision.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace GigeVision.Core.Models
{
    /// <summary>
    /// different class for Integer Register type
    /// </summary>
    public class MaskedIntReg
    {
        /// <summary>
        /// Integer Register Address
        /// </summary>
        public string Address { get; private set; }

        /// <summary>
        /// Integer Register Address Parameter
        /// </summary>
        public IntSwissKnife AddressParameter { get; set; }

        /// <summary>
        /// Integer Register Length
        /// </summary>
        public uint Length { get; private set; }

        /// <summary>
        /// Integer Register Access Mode
        /// </summary>
        public CameraRegisterAccessMode AccessMode { get; private set; }

        /// <summary>
        /// Integer Register Value
        /// </summary>
        public uint? Value { get; set; }

        /// <summary>
        /// Camera Register has Integer address, length and access mode
        /// </summary>
        public CameraRegister Register { get; set; }

        /// <summary>
        /// Main Method
        /// </summary>
        /// <param name="pAddress"></param>
        /// <param name="address"></param>
        /// <param name="length"></param>
        /// <param name="accessMode"></param>
        public MaskedIntReg(IntSwissKnife adddressParameter, string address, uint length, CameraRegisterAccessMode accessMode)
        {
            AddressParameter = adddressParameter;
            Address = address;
            Length = length;
            AccessMode = accessMode;
            Register = new CameraRegister(address, length, accessMode, null, AddressParameter);
        }
    }
}