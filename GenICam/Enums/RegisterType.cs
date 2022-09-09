using System;
using System.Collections.Generic;
using System.Text;

namespace GenICam
{
    /// <summary>
    /// Register type.
    /// </summary>
    public enum RegisterType
    {
        /// <summary>Integer register.</summary>
        IntReg,

        /// <summary>Integer.</summary>
        Integer,

        /// <summary>Masked integer register.</summary>
        MaskedIntReg,

        /// <summary>Float.</summary>
        Float,

        /// <summary>Float register.</summary>
        FloatReg,

        /// <summary>Structure register.</summary>
        StructReg,
    }
}