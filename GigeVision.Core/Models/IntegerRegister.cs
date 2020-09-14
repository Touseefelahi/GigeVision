using System;
using System.Collections.Generic;
using System.Text;

namespace GigeVision.Core.Models
{
    public class IntegerRegister
    {
        public int Min { get; private set; }
        public int Max { get; private set; }
        public int Inc { get; private set; }

        public CameraRegister Register { get; private set; }

        public IntegerRegister(int min, int max, int inc, CameraRegister register)
        {
            Min = min;
            Max = max;
            Inc = inc;
            Register = register;
        }
    }
}