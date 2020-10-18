using System;
using System.Collections.Generic;
using System.Text;

namespace GigeVision.Core.Models
{
    /// <summary>
    /// Boolean Register
    /// </summary>
    public class BooleanRegister
    {
        /// <summary>
        /// Camera Register has boolean address, length and access mode
        /// </summary>
        public CameraRegister Register { get; private set; }

        /// <summary>
        /// the main method
        /// </summary>
        /// <param name="cameraRegister">camera Register has boolean address, length and access mode</param>
        public BooleanRegister(CameraRegister cameraRegister)
        {
            Register = cameraRegister;
        }
    }
}