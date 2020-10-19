using System;
using System.Collections.Generic;
using System.Text;

namespace GigeVision.Core.Models
{
    /// <summary>
    /// Integer Register
    /// </summary>
    public class IntegerRegister
    {
        /// <summary>
        /// Integer Value
        /// </summary>
        public double? Value { get; private set; }

        /// <summary>
        /// Integer Minimum Value
        /// </summary>
        public double? Min { get; private set; }

        /// <summary>
        /// Integer Maximum Value
        /// </summary>
        public double? Max { get; private set; }

        /// <summary>
        /// Integer Increment Value
        /// </summary>
        public double? Inc { get; private set; }

        /// <summary>
        /// Integer Minimum Parameter
        /// </summary>
        public IntSwissKnife MinParameter { get; set; }

        /// <summary>
        /// Integer Maximum Parameter
        /// </summary>
        public IntSwissKnife MaxParameter { get; set; }

        /// <summary>
        /// Integer Value Parameter
        /// </summary>
        public object ValueParameter { get; set; }

        /// <summary>
        /// Camera Register has Integer address, length and access mode
        /// </summary>
        public CameraRegister Register { get; private set; }

        /// <summary>
        /// Main Method
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="inc"></param>
        /// <param name="value"></param>
        /// <param name="register"></param>
        /// <param name="pValue"></param>
        /// <param name="pMin"></param>
        /// <param name="pMax"></param>
        public IntegerRegister(double? min = null, double? max = null, double? inc = null, double? value = null, CameraRegister register = null, object pValue = null, IntSwissKnife pMin = null, IntSwissKnife pMax = null)
        {
            Min = min;
            Max = max;
            Inc = inc;
            Value = value;
            Register = register;
            ValueParameter = pValue;
            MinParameter = pMin;
            MaxParameter = pMax;

            //DTOs
            if (MaxParameter != null)
                Max = MaxParameter.Value;
            if (MinParameter != null)
                Min = MinParameter.Value;
            if (MaxParameter != null)
                Max = MaxParameter.Value;
            if (ValueParameter is IntSwissKnife intSwissKnife)
                Value = intSwissKnife.Value;
            if (ValueParameter is MaskedIntReg maskedIntReg)
                Value = maskedIntReg.Value;
        }
    }
}