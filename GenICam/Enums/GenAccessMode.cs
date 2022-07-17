using System;
using System.Collections.Generic;
using System.Text;

namespace GenICam
{
    public enum GenAccessMode
    {
        /// <summary>
        /// Not Implemented
        /// </summary>
        NI,
        /// <summary>
        /// Not Available
        /// </summary>
        NA,
        /// <summary>
        /// Write Only
        /// </summary>
        WO,
        /// <summary>
        /// Read Only
        /// </summary>
        RO,
        /// <summary>
        /// Readable and Writable
        /// </summary>
        RW
    }
}