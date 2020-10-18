using GigeVision.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace GigeVision.Core.Models
{
    /// <summary>
    /// Float Register
    /// </summary>
    public class FloatRegister
    {
        /// <summary>
        /// Float Minimum Parameter
        /// </summary>
        public IntSwissKnife MinParameter { get; set; }

        /// <summary>
        /// Float Maximum Parameter
        /// </summary>
        public IntSwissKnife MaxParameter { get; set; }

        /// <summary>
        /// Float Value Parameter
        /// </summary>
        public IntSwissKnife ValueParameter { get; set; }

        /// <summary>
        /// Float Physical Unit
        /// </summary>
        public PhysicalUnit? PhysicalUnit { get; private set; }

        /// <summary>
        /// Main Method
        /// </summary>
        /// <param name="pValue"></param>
        /// <param name="pMin"></param>
        /// <param name="pMax"></param>
        /// <param name="physicalUnit"></param>
        public FloatRegister(IntSwissKnife pValue, IntSwissKnife pMin = null, IntSwissKnife pMax = null, PhysicalUnit? physicalUnit = null)
        {
            MinParameter = pMin;
            MaxParameter = pMax;
            PhysicalUnit = physicalUnit;
        }
    }
}