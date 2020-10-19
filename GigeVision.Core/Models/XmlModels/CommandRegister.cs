using System;
using System.Collections.Generic;
using System.Text;

namespace GigeVision.Core.Models
{
    /// <summary>
    /// Command Register
    /// </summary>
    public class CommandRegister
    {
        /// <summary>
        /// Command Value
        /// </summary>
        public uint Value { get; private set; }

        /// <summary>
        /// Camera Register has Command address, length and access mode
        /// </summary>
        public CameraRegister Register { get; private set; }

        /// <summary>
        /// Main Method
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cameraRegister"></param>
        public CommandRegister(uint value, CameraRegister cameraRegister)
        {
            Register = cameraRegister;
            Value = value;
        }
    }
}