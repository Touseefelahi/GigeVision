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
        private double? max;

        /// <summary>
        /// Integer Minimum Value
        /// </summary>
        public double? Min { get; private set; }

        /// <summary>
        /// Integer Maximum Value
        /// </summary>
        public double? Max
        {
            get
            {
                if (max is null)
                    return uint.MaxValue;

                return max;
            }
            private set => max = value;
        }

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

        public double? Value { get; }

        /// <summary>
        /// Camera Register has Integer address, length and access mode
        /// </summary>

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
        public IntegerRegister(double? min = null, double? max = null, double? inc = null, object pValue = null, IntSwissKnife pMin = null, IntSwissKnife pMax = null)
        {
            Min = min == null ? 1 : min;
            Max = max;
            Inc = inc;
            ValueParameter = pValue;
            MinParameter = pMin;
            MaxParameter = pMax;

            //DTOs
            if (MaxParameter != null)
                if (MaxParameter.Value != null)
                    Max = double.Parse(MaxParameter.Value.ToString());
            if (MinParameter != null)
                if (MinParameter.Value != null)
                    Min = double.Parse(MinParameter.Value.ToString());

            if (Value is null)
            {
                if (ValueParameter is IntSwissKnife intSwissKnife)
                    if (intSwissKnife.Value != null)
                        Value = double.Parse(intSwissKnife.Value.ToString());

                if (ValueParameter is MaskedIntReg maskedIntReg)
                    Value = maskedIntReg.Value;
            }
        }
    }
}