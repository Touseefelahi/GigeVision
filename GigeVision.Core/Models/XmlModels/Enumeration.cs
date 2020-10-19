using System;
using System.Collections.Generic;
using System.Text;

namespace GigeVision.Core.Models
{
    /// <summary>
    /// Enumeration Register
    /// </summary>
    public class Enumeration
    {
        /// <summary>
        /// Enumeration Entry List
        /// </summary>
        public Dictionary<string, uint> Entry { get; private set; }

        /// <summary>
        ///  Camera Register has Enumeration address, length and access mode
        /// </summary>
        public CameraRegister Register { get; private set; }

        /// <summary>
        /// Main Method
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="register"></param>
        public Enumeration(Dictionary<string, uint> entry, CameraRegister register)
        {
            Entry = entry;
            Register = register;
        }
    }
}