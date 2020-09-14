using GigeVision.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace GigeVision.Core.Models
{
    public class FloatRegister
    {
        public IntSwissKnife Min { get; private set; }
        public IntSwissKnife Max { get; private set; }
        public IntSwissKnife Value { get; private set; }

        public PhysicalUnit? PhysicalUnit { get; private set; }

        public FloatRegister(IntSwissKnife value, IntSwissKnife min = null, IntSwissKnife max = null, PhysicalUnit? physicalUnit = null)
        {
            Min = min;
            Max = max;
            PhysicalUnit = physicalUnit;
        }
    }
}