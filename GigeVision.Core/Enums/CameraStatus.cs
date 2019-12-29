using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigeVision.Core.Enums
{
    public enum CameraStatus
    {
        /// <summary>
        /// Camera Available in network and its not in Control/Streaming
        /// </summary>
        Available,

        /// <summary>
        /// Camera available in network and its in control
        /// </summary>
        InControl,

        /// <summary>
        /// Camera not found in network
        /// </summary>
        UnAvailable
    }
}